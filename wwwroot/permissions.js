// Reusable action bar: asks the API which permissions the current user holds, then enables only
// those buttons. Uses the MVC controller (/mvc/me); the minimal-API /api/me returns the same shape.
// The API owns the role→permission logic; the frontend just matches "action:resource" strings.

const ACTIONS = [
	{ permission: "read:posts", label: "Browse posts", href: "/" },
	{ permission: "create:posts", label: "Write a post", href: "/Posts/Create" },
	{ permission: "edit:posts", label: "Edit any post", href: "/" },
	{ permission: "delete:posts", label: "Delete any post", href: "/" },
	{
		permission: "manage:access",
		label: "Manage access",
		href: "/Admin/Access",
	},
];

// The only permission logic on the frontend: does the API's list contain this "action:resource"?
function can(permissions, permission) {
	return permissions.includes(permission);
}

// eslint-disable-next-line no-unused-vars
async function renderPermissionBar(containerId) {
	const el = document.getElementById(containerId);
	const res = await fetch("/mvc/me", {
		headers: { Accept: "application/json" },
	});
	if (res.status === 401 || res.status === 403) {
		el.innerHTML = `<p class="muted"><a href="/Login">Sign in</a> to see which actions you can take.</p>`;
		return;
	}
	if (!res.ok) {
		el.innerHTML = `<p class="muted">Couldn't load your permissions.</p>`;
		return;
	}

	const { user, provider, permissions } = await res.json();
	const buttons = ACTIONS.map((action) => {
		if (can(permissions, action.permission))
			return `<a class="btn btn--primary" href="${action.href}">${action.label}</a>`;
		return `<button class="btn btn--secondary" disabled title="Needs the ${action.permission} permission">${action.label} 🔒</button>`;
	}).join("");

	el.innerHTML = `<p class="muted">Signed in as <strong>${user}</strong> using <strong>${provider}</strong> — buttons reflect the permissions the API returned:</p>
		<div class="btn-row">${buttons}</div>`;
}
