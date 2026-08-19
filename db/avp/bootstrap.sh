#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
project_dir=$(cd -- "$script_dir/../.." && pwd)
configured_region=$(sed -n 's/^AWS_REGION=//p' "$project_dir/.env")
configured_store=$(sed -n 's/^AVP_POLICY_STORE_ID=//p' "$project_dir/.env")
aws_region=${AWS_REGION:-$configured_region}
policy_store_alias=${AVP_POLICY_STORE_ID:-${configured_store:-policy-store-alias/fga-blog-poc}}
: "${aws_region:?Set AWS_REGION in the environment or .env}"

if [[ $policy_store_alias != policy-store-alias/* ]]; then
  echo "AVP_POLICY_STORE_ID must use the policy-store-alias/ prefix." >&2
  exit 1
fi

for command_name in aws jq sha256sum; do
  if ! command -v "$command_name" >/dev/null; then
    echo "$command_name is required." >&2
    exit 1
  fi
done

if ! aws verifiedpermissions get-policy-store-alias help >/dev/null 2>&1; then
  echo "This bootstrap requires an AWS CLI v2 release with Verified Permissions policy-store alias commands." >&2
  echo "Update the AWS CLI, then rerun this script." >&2
  exit 1
fi

bootstrap_tmp_dir=$(mktemp -d)
trap 'rm -rf -- "$bootstrap_tmp_dir"' EXIT

avp() {
  aws --region "$aws_region" --no-cli-pager verifiedpermissions "$@"
}

retry() {
  for _retry_attempt in 1 2 3 4 5 6 7 8 9 10; do
    if "$@"; then
      return 0
    fi
    sleep 1
  done
  return 1
}

policy_store_id=$(
  avp get-policy-store-alias \
    --alias-name "$policy_store_alias" \
    --query policyStoreId \
    --output text 2>/dev/null || true
)

if [[ -z $policy_store_id || $policy_store_id == None ]]; then
  policy_store_id=$(avp create-policy-store \
    --validation-settings mode=OFF \
    --description "Trailhead switchable authorization POC" \
    --query policyStoreId \
    --output text)
  retry avp create-policy-store-alias \
    --alias-name "$policy_store_alias" \
    --policy-store-id "$policy_store_id" >/dev/null
  echo "Created policy store $policy_store_id as $policy_store_alias"
else
  echo "Reconciling policy store $policy_store_id ($policy_store_alias)"
fi

jq -n --rawfile cedar_json "$script_dir/schema.json" \
  '{cedarJson: $cedar_json}' >"$bootstrap_tmp_dir/schema-definition.json"
retry avp put-schema \
  --policy-store-id "$policy_store_alias" \
  --definition "file://$bootstrap_tmp_dir/schema-definition.json" >/dev/null
retry avp update-policy-store \
  --policy-store-id "$policy_store_alias" \
  --validation-settings mode=STRICT >/dev/null

for role in admin editor writer reader; do
  template_name="name/blog-role-$role"
  if avp get-policy-template \
    --policy-store-id "$policy_store_alias" \
    --policy-template-id "$template_name" >/dev/null 2>&1; then
    retry avp update-policy-template \
      --policy-store-id "$policy_store_alias" \
      --policy-template-id "$template_name" \
      --statement "file://$script_dir/templates/$role.cedar" \
      --description "Trailhead $role role" >/dev/null
  else
    retry avp create-policy-template \
      --policy-store-id "$policy_store_alias" \
      --name "$template_name" \
      --statement "file://$script_dir/templates/$role.cedar" \
      --description "Trailhead $role role" >/dev/null
  fi
done

owner_policy_name=name/post-owner
jq -n \
  --arg policyStoreId "$policy_store_alias" \
  --arg name "$owner_policy_name" \
  --rawfile statement "$script_dir/policies/post-owner.cedar" \
  '{policyStoreId: $policyStoreId, name: $name, definition: {static: {statement: $statement}}}' \
  >"$bootstrap_tmp_dir/create-owner.json"
jq '{policyStoreId, policyId: .name, definition}' \
  "$bootstrap_tmp_dir/create-owner.json" >"$bootstrap_tmp_dir/update-owner.json"

if avp get-policy \
  --policy-store-id "$policy_store_alias" \
  --policy-id "$owner_policy_name" >/dev/null 2>&1; then
  retry avp update-policy \
    --cli-input-json "file://$bootstrap_tmp_dir/update-owner.json" >/dev/null
else
  retry avp create-policy \
    --cli-input-json "file://$bootstrap_tmp_dir/create-owner.json" >/dev/null
fi

while IFS=$'\t' read -r username role; do
  grant_hash=$(printf '%s\0%s' "$username" "$role" | sha256sum | cut -d' ' -f1)
  grant_name="name/role-grant-$grant_hash"
  if avp get-policy \
    --policy-store-id "$policy_store_alias" \
    --policy-id "$grant_name" >/dev/null 2>&1; then
    continue
  fi

  jq -n \
    --arg policyStoreId "$policy_store_alias" \
    --arg name "$grant_name" \
    --arg clientToken "$grant_hash" \
    --arg template "name/blog-role-$role" \
    --arg username "$username" \
    '{
      policyStoreId: $policyStoreId,
      name: $name,
      clientToken: $clientToken,
      definition: {templateLinked: {
        policyTemplateId: $template,
        principal: {entityType: "Trailhead::User", entityId: $username},
        resource: {entityType: "Trailhead::Blog", entityId: "main"}
      }}
    }' >"$bootstrap_tmp_dir/seed-grant.json"
  retry avp create-policy \
    --cli-input-json "file://$bootstrap_tmp_dir/seed-grant.json" >/dev/null
done < <(jq -r '.[] | [.username, .role] | @tsv' "$script_dir/seed.json")

echo "Amazon Verified Permissions is ready."
echo "AUTHORIZATION_PROVIDER=verifiedpermissions"
echo "AWS_REGION=$aws_region"
echo "AVP_POLICY_STORE_ID=$policy_store_alias"
