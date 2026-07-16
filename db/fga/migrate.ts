#!/usr/bin/env bun
/**
 * FGA "migration": provisions the OpenFGA store from db/fga (model.dsl + seed.json) via the
 * REST API. Run by the fga-migrate compose service (oven/bun). Idempotent: drops any prior
 * store of this name, then recreates it fresh with the model and seed tuples.
 * (`fga store import` always creates a new store, so this replaces the CLI + reset shell.)
 */
import { transformer } from "@openfga/syntax-transformer";

interface Store {
	id: string;
	name: string;
}

const apiUrl = (process.env.FGA_API_URL ?? "http://localhost:8080").replace(
	/\/$/,
	"",
);
const storeName = process.env.FGA_STORE_NAME ?? "fga-blog-poc";

async function api<T>(path: string, init?: RequestInit): Promise<T> {
	const res = await fetch(`${apiUrl}${path}`, {
		...init,
		headers: { "content-type": "application/json", ...init?.headers },
	});
	if (!res.ok)
		throw new Error(
			`${init?.method ?? "GET"} ${path} → ${res.status} ${await res.text()}`,
		);
	return res.json() as Promise<T>;
}

// 1. Reset: drop any existing store(s) with this name so the import stays the source of truth.
const { stores } = await api<{ stores: Store[] }>("/stores");
for (const stale of stores.filter((s) => s.name === storeName)) {
	await fetch(`${apiUrl}/stores/${stale.id}`, { method: "DELETE" });
	console.log(`removed stale store ${stale.id}`);
}

// 2. Create the store fresh.
const store = await api<Store>("/stores", {
	method: "POST",
	body: JSON.stringify({ name: storeName }),
});

// 3. Write the authorization model, transforming the DSL source to the JSON the API expects.
const dsl = await Bun.file(`${import.meta.dir}/model.dsl`).text();
const model = JSON.parse(transformer.transformDSLToJSON(dsl));
const { authorization_model_id } = await api<{
	authorization_model_id: string;
}>(`/stores/${store.id}/authorization-models`, {
	method: "POST",
	body: JSON.stringify(model),
});

// 4. Write the seed tuples.
const seedTuples = await Bun.file(`${import.meta.dir}/seed.json`).json();
await api(`/stores/${store.id}/write`, {
	method: "POST",
	body: JSON.stringify({
		authorization_model_id,
		writes: { tuple_keys: seedTuples },
	}),
});

console.log(
	`imported store ${store.name} (${store.id}) — model ${authorization_model_id}, ${seedTuples.length} tuples`,
);
