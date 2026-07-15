using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace FgaPoc.Fga;

/// <summary>
/// The Trailhead authorization model, built from the SDK's type-definition objects.
/// This is the runtime source of truth; <c>fga/model.fga</c> is a hand-kept DSL mirror.
/// </summary>
public static class FgaModel
{
    public static ClientWriteAuthorizationModelRequest Build() =>
        new()
        {
            SchemaVersion = "1.1",
            TypeDefinitions =
            [
                new TypeDefinition { Type = "user", Relations = new Dictionary<string, Userset>() },
                BlogType(),
                PostType(),
            ],
        };

    // blog: nested roles admin ⊃ editor ⊃ writer ⊃ reader, each directly assignable to users.
    private static TypeDefinition BlogType() =>
        new()
        {
            Type = "blog",
            Relations = new Dictionary<string, Userset>
            {
                ["admin"] = Direct(),
                ["editor"] = DirectOr("admin"),
                ["writer"] = DirectOr("editor"),
                ["reader"] = DirectOr("writer"),
            },
            Metadata = DirectUserMetadata("admin", "editor", "writer", "reader"),
        };

    // post: linked to a blog + an owner; permissions inherit from the blog roles.
    private static TypeDefinition PostType() =>
        new()
        {
            Type = "post",
            Relations = new Dictionary<string, Userset>
            {
                ["blog"] = Direct(),
                ["owner"] = Direct(),
                ["can_read"] = FromBlog("reader"),
                ["can_edit"] = OwnerOrFromBlog("editor"),
                ["can_delete"] = OwnerOrFromBlog("editor"),
            },
            Metadata = new Metadata
            {
                Relations = new Dictionary<string, RelationMetadata>
                {
                    ["blog"] = RelatedTo("blog"),
                    ["owner"] = RelatedTo("user"),
                },
            },
        };

    private static Userset Direct() => new() { This = new object() };

    // "[user] or <relation>" — directly assignable, or inherited from a higher role.
    private static Userset DirectOr(string computedRelation) =>
        new()
        {
            Union = new Usersets
            {
                Child =
                [
                    new Userset { This = new object() },
                    new Userset
                    {
                        ComputedUserset = new ObjectRelation { Relation = computedRelation },
                    },
                ],
            },
        };

    // "<relation> from blog" — pull the given blog role through the post's blog link.
    private static Userset FromBlog(string blogRelation) =>
        new()
        {
            TupleToUserset = new TupleToUserset
            {
                Tupleset = new ObjectRelation { Relation = "blog" },
                ComputedUserset = new ObjectRelation { Relation = blogRelation },
            },
        };

    // "owner or <relation> from blog" — the post owner, or anyone with the blog role.
    private static Userset OwnerOrFromBlog(string blogRelation) =>
        new()
        {
            Union = new Usersets
            {
                Child =
                [
                    new Userset { ComputedUserset = new ObjectRelation { Relation = "owner" } },
                    FromBlog(blogRelation),
                ],
            },
        };

    private static Metadata DirectUserMetadata(params string[] relations) =>
        new() { Relations = relations.ToDictionary(r => r, _ => RelatedTo("user")) };

    private static RelationMetadata RelatedTo(string type) =>
        new() { DirectlyRelatedUserTypes = [new RelationReference { Type = type }] };
}
