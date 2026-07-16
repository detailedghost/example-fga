model
  schema 1.1

type user

type blog
  relations
    define admin: [user]
    define editor: [user] or admin
    define writer: [user] or editor
    define reader: [user] or writer

type post
  relations
    define blog: [blog]
    define owner: [user]
    define can_read: reader from blog
    define can_edit: owner or editor from blog
    define can_delete: owner or editor from blog
