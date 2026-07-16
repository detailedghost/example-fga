-- Blog schema. Roles live in OpenFGA; these tables hold only identity + content.

CREATE TABLE IF NOT EXISTS users (
    id serial PRIMARY KEY,
    username text UNIQUE NOT NULL,
    password text NOT NULL,
    display_name text NOT NULL
);

CREATE TABLE IF NOT EXISTS posts (
    id serial PRIMARY KEY,
    title text NOT NULL,
    body text NOT NULL,
    author_username text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
