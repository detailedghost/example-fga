// Local stand-in for the bundled OpenFGA Playground: renders the seeded store's model and
// tuples and runs Checks. The app hands over the store coordinates (/api/fga/store) so the
// FGA API URL stays in .env; every read below is a plain call to that API.

let store = null;

function setStatus(message, kind = "info") {
	const el = document.getElementById("status");
	el.className = `alert alert--${kind}`;
	el.textContent = message;
	el.hidden = false;
}

function heading(section, text) {
	const h = document.createElement("h2");
	h.textContent = text;
	section.append(h);
}

async function fgaGet(path) {
	const res = await fetch(`${store.apiUrl}${path}`);
	if (!res.ok) throw new Error(`GET ${path} → ${res.status}`);
	return res.json();
}

async function fgaPost(path, body) {
	const res = await fetch(`${store.apiUrl}${path}`, {
		method: "POST",
		headers: { "content-type": "application/json" },
		body: JSON.stringify(body),
	});
	if (!res.ok) throw new Error(`POST ${path} → ${res.status}`);
	return res.json();
}

function renderStore() {
	const section = document.getElementById("store");
	heading(section, "Store");
	const table = document.createElement("table");
	table.className = "table";
	for (const [label, value] of [
		["Name", store.storeName],
		["Store id", store.storeId],
		["Model id", store.modelId],
		["API", store.apiUrl],
	]) {
		const row = table.insertRow();
		row.insertCell().textContent = label;
		const cell = row.insertCell();
		const code = document.createElement("code");
		code.textContent = value;
		cell.append(code);
	}
	section.append(table);
}

// Relation names per type, so the Check form can offer real relations instead of free text.
function renderModel(model) {
	const section = document.getElementById("model");
	heading(section, "Authorization model");
	const table = document.createElement("table");
	table.className = "table";
	const head = table.createTHead().insertRow();
	for (const label of ["Type", "Relations"]) {
		const th = document.createElement("th");
		th.textContent = label;
		head.append(th);
	}
	for (const def of model.type_definitions) {
		const relations = Object.keys(def.relations ?? {});
		const row = table.insertRow();
		row.insertCell().textContent = def.type;
		row.insertCell().textContent =
			relations.length > 0 ? relations.join(", ") : "(no relations)";
	}
	section.append(table);
	return model.type_definitions.flatMap((d) => Object.keys(d.relations ?? {}));
}

function renderTuples(tuples) {
	const section = document.getElementById("tuples");
	heading(section, `Tuples (${tuples.length})`);
	const table = document.createElement("table");
	table.className = "table";
	const head = table.createTHead().insertRow();
	for (const label of ["User", "Relation", "Object"]) {
		const th = document.createElement("th");
		th.textContent = label;
		head.append(th);
	}
	for (const { key } of tuples) {
		const row = table.insertRow();
		row.insertCell().textContent = key.user;
		row.insertCell().textContent = key.relation;
		row.insertCell().textContent = key.object;
	}
	section.append(table);
}

function datalist(id, values) {
	const list = document.createElement("datalist");
	list.id = id;
	for (const value of [...new Set(values)].sort()) {
		const option = document.createElement("option");
		option.value = value;
		list.append(option);
	}
	return list;
}

function field(labelText, control) {
	const wrap = document.createElement("label");
	wrap.className = "field";
	const label = document.createElement("span");
	label.className = "field__label";
	label.textContent = labelText;
	wrap.append(label, control);
	return wrap;
}

function renderCheck(relations, tuples) {
	const section = document.getElementById("check");
	heading(section, "Check");

	const user = document.createElement("input");
	user.className = "input";
	user.setAttribute("list", "pg-users");
	user.value = "user:dave";

	const relation = document.createElement("select");
	relation.className = "input";
	for (const name of [...new Set(relations)].sort()) {
		const option = document.createElement("option");
		option.value = name;
		option.textContent = name;
		relation.append(option);
	}
	relation.value = "can_edit";

	const object = document.createElement("input");
	object.className = "input";
	object.setAttribute("list", "pg-objects");
	object.value = "post:1";

	const run = document.createElement("button");
	run.className = "btn btn--primary";
	run.type = "submit";
	run.textContent = "Run check";

	const result = document.createElement("div");
	result.hidden = true;

	const form = document.createElement("form");
	form.className = "form";
	form.append(
		field("User", user),
		field("Relation", relation),
		field("Object", object),
		datalist(
			"pg-users",
			tuples.map((t) => t.key.user),
		),
		datalist(
			"pg-objects",
			tuples.map((t) => t.key.object),
		),
		run,
		result,
	);

	form.addEventListener("submit", async (event) => {
		event.preventDefault();
		run.disabled = true;
		try {
			const body = {
				tuple_key: {
					user: user.value.trim(),
					relation: relation.value,
					object: object.value.trim(),
				},
				authorization_model_id: store.modelId,
			};
			const { allowed } = await fgaPost(`/stores/${store.storeId}/check`, body);
			result.className = `alert alert--${allowed ? "success" : "danger"}`;
			result.textContent = `${body.tuple_key.user} ${body.tuple_key.relation} ${body.tuple_key.object} → ${allowed ? "ALLOWED" : "DENIED"}`;
			result.hidden = false;
		} catch (error) {
			result.className = "alert alert--danger";
			result.textContent = String(error);
			result.hidden = false;
		} finally {
			run.disabled = false;
		}
	});

	section.append(form);
}

async function load() {
	const res = await fetch("/api/fga/store");
	if (res.status === 401) {
		setStatus("Sign in first — then reload this page.", "warning");
		return;
	}
	if (!res.ok) {
		setStatus(`Could not read the store: ${res.status}`, "danger");
		return;
	}
	store = await res.json();

	try {
		const [{ authorization_model: model }, { tuples }] = await Promise.all([
			fgaGet(`/stores/${store.storeId}/authorization-models/${store.modelId}`),
			fgaPost(`/stores/${store.storeId}/read`, {}),
		]);
		renderStore();
		renderCheck(renderModel(model), tuples);
		renderTuples(tuples);
	} catch (error) {
		setStatus(
			`Reached the app but not the OpenFGA API at ${store.apiUrl} — is docker compose up? (${error})`,
			"danger",
		);
	}
}

load();
