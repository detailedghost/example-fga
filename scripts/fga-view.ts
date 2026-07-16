#!/usr/bin/env bun
// Views the dummy app's OpenFGA store — its model relations and every tuple —
// straight from the REST API on FGA_API_URL. No playground, no bridge; just the
// port that docker-compose already publishes. Bun auto-loads the repo-root .env.

const apiUrl = (process.env.FGA_API_URL ?? "http://localhost:8080").replace(
	/\/$/,
	"",
);
const wantStore = process.env.FGA_STORE_NAME ?? "fga-blog-poc";

interface Store {
	id: string;
	name: string;
	created_at: string;
}

interface TupleKey {
	user: string;
	relation: string;
	object: string;
}

async function getJson<T>(path: string, init?: RequestInit): Promise<T> {
	const res = await fetch(`${apiUrl}${path}`, init);
	if (!res.ok)
		throw new Error(
			`${init?.method ?? "GET"} ${path} → ${res.status} ${res.statusText}`,
		);
	return res.json() as Promise<T>;
}

const { stores } = await getJson<{ stores: Store[] }>("/stores");
if (stores.length === 0) {
	console.log("No stores found. Is `dotnet run` bootstrapped yet?");
	process.exit(0);
}

const store = stores.find((s) => s.name === wantStore) ?? stores[0];
console.log(`\nStore  ${store.name}  (${store.id})`);
console.log(`Made   ${store.created_at}\n`);

const { authorization_models } = await getJson<{
	authorization_models: {
		id: string;
		type_definitions: { type: string; relations?: Record<string, unknown> }[];
	}[];
}>(`/stores/${store.id}/authorization-models`);
const model = authorization_models[0];
console.log(`Model  ${model.id}`);
for (const def of model.type_definitions) {
	const relations = Object.keys(def.relations ?? {});
	const shown = relations.length > 0 ? relations.join(", ") : "(no relations)";
	console.log(`  type ${def.type.padEnd(6)} → ${shown}`);
}

const { tuples } = await getJson<{ tuples: { key: TupleKey }[] }>(
	`/stores/${store.id}/read`,
	{
		method: "POST",
		headers: { "content-type": "application/json" },
		body: "{}",
	},
);

console.log(`\nTuples (${tuples.length})`);
const byObject = Map.groupBy(tuples, (t) => t.key.object);
for (const [object, group] of [...byObject].sort()) {
	console.log(`  ${object}`);
	for (const { key } of group)
		console.log(
			`    ${key.user.padEnd(14)} ${key.relation.padEnd(10)} ${key.object}`,
		);
}
console.log();
