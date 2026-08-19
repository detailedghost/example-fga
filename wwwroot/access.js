// Framework-free frontend for the selected provider's role matrix.
// Reads state from GET /api/access; toggles call the same grant/revoke endpoints the app uses.

const statusEl = document.getElementById("status");
const gridEl = document.getElementById("grid");
const providerEl = document.getElementById("provider");

function showStatus(message, kind) {
	statusEl.textContent = message;
	statusEl.className = `alert alert--${kind}`;
	statusEl.hidden = false;
}

// Highest directly-granted role. `order` is the API's role list (admin→reader), so nothing is hardcoded.
function effectiveRole(userRoles, order) {
	return order.find((role) => userRoles.includes(role)) ?? null;
}

async function load() {
	const res = await fetch("/api/access", {
		headers: { Accept: "application/json" },
	});
	if (res.status === 401 || res.status === 403) {
		gridEl.innerHTML = `<p class="muted">You need to <a href="/Login">sign in as an admin</a> to view this.</p>`;
		return;
	}
	if (!res.ok) {
		showStatus("Failed to load access data.", "danger");
		return;
	}
	render(await res.json());
}

function render(data) {
	providerEl.textContent = `Active provider: ${data.provider}`;
	const header = data.roles.map((role) => `<th>${role}</th>`).join("");
	const rows = data.users
		.map((user) => {
			const cells = data.roles
				.map((role) => {
					const selfAdmin =
						role === "admin" && user.username === data.currentUser;
					const title = selfAdmin
						? "You can't revoke your own admin"
						: `${role} for ${user.username}`;
					return `<td><label class="toggle" title="${title}">
                        <input type="checkbox" class="access-toggle"
                               data-username="${user.username}" data-role="${role}"
                               ${user.roles.includes(role) ? "checked" : ""} ${selfAdmin ? "disabled" : ""} />
                        <span class="toggle__track"></span></label></td>`;
				})
				.join("");
			const role = effectiveRole(user.roles, data.roles);
			const pill = role
				? `<span class="role-pill" data-role="${role}">${role}</span>`
				: `<span class="muted">none</span>`;
			return `<tr><td>${user.username}</td>${cells}<td>${pill}</td></tr>`;
		})
		.join("");

	gridEl.innerHTML = `<div class="table-wrap"><table class="table access-matrix">
        <thead><tr><th>User</th>${header}<th>Effective</th></tr></thead>
        <tbody>${rows}</tbody></table></div>`;

	for (const toggle of gridEl.querySelectorAll(".access-toggle"))
		toggle.addEventListener("change", onToggle);
}

async function onToggle(event) {
	const toggle = event.currentTarget;
	const { username, role } = toggle.dataset;
	const grant = toggle.checked;
	toggle.disabled = true;
	try {
		const res = await fetch(`/api/access/${grant ? "grant" : "revoke"}`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({ username, role }),
		});
		if (!res.ok) throw new Error(String(res.status));
		showStatus(
			`${grant ? "Granted" : "Revoked"} ${role} for ${username}.`,
			"success",
		);
		await load(); // re-read the authoritative state from the selected provider
	} catch {
		showStatus(`Could not update ${role} for ${username}.`, "danger");
		toggle.checked = !grant;
		toggle.disabled = false;
	}
}

load();
