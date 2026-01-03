import { useEffect, useMemo, useState } from "react";
import { createOrder, createUser, listOrdersByUser, listUsers, type OrderResponseDto, type UserResponseDto } from "./api";
import './App.css'

type Page = "users" | "orders"

export default function App() {
  const [page, setPage] = useState<Page>("users");
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [error, setError] = useState<string>("");

  const selectedUser = useMemo(
    () => users.find(u => u.id === selectedUserId) ?? null,
    [users, selectedUserId]
  );

  async function refreshUsers() {
    const data = await listUsers();
    setUsers(Array.isArray(data) ? data : []);
  }

  useEffect(() => {
    refreshUsers().catch(e => setError(e.message));
  }, []);

  function selectUser(id: string) {
    setSelectedUserId(id);
    setPage("orders");
    setError("");
  }

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebarHeader">
          <div className="title">ECommerce Demo</div>
        </div>

        <div className="sidebarSectionLabel">Users</div>
        <div className="userList">
          {users.length === 0 ? (
            <div className="muted">No users yet</div>
          ) : (
            users.map(u => (
              <button
                key={u.id}
                className={"userItem " + (u.id === selectedUserId ? "active" : "")}
                onClick={() => selectUser(u.id)}
                title={u.id}
              >
                <div className="userName">{u.name.length > 15 ? u.name.slice(0, 15) + "..." : u.name}</div>
                <div className="userEmail">{u.email.length > 20 ? u.email.slice(0, 15) + "..." : u.email}</div>
              </button>
            ))
          )}
        </div>
      </aside>

      <main className="main">
        <div className="topBar">
          <div className="crumb">
            {page === "users" ? "Create User" : `Create Order${selectedUser ? ` for ${selectedUser.name.length > 15 ? selectedUser.name.slice(0, 15) + "..." : selectedUser.name} (${selectedUser.email.length > 15 ? selectedUser.email.slice(0, 15) + "..." : selectedUser.email})` : ""}`}
          </div>

          {page === "orders" && (
            <button className="btn" onClick={() => setPage("users")}>
              ← Back to Users
            </button>
          )}
        </div>

        {error ? <div className="alert">{error}</div> : null}

        {page === "users" ? (
          <UserCreatePanel
            onCreated={async (u) => {
              await refreshUsers();
              setSelectedUserId(u.id);
              setPage("orders");
            }}
            onError={setError}
          />
        ) : (
          <OrderCreatePanel user={selectedUser} onError={setError} />
        )}
      </main>
    </div>
  );
}

function UserCreatePanel(props: { onCreated: (u: UserResponseDto) => Promise<void>; onError: (m: string) => void }) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    props.onError("");
    setBusy(true);

    try {
      const created = await createUser({ name, email });
      setName("");
      setEmail("");
      await props.onCreated(created);
    } catch (err) {
      props.onError((err as Error).message ?? "Failed to create user");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="card">
      <div className="cardTitle">Create a user</div>
      <form onSubmit={submit} className="form">
        <label className="label">
          Name
          <input className="input" value={name} onChange={e => setName(e.target.value)} disabled={busy} />
        </label>

        <label className="label">
          Email
          <input className="input" value={email} onChange={e => setEmail(e.target.value)} disabled={busy} />
        </label>

        <button className="btnPrimary" disabled={busy}>
          {busy ? "Creating..." : "Create User"}
        </button>
      </form>

      <div className="hint">After creating, the user is auto-selected for order creation.</div>
    </div>
  );
}

function OrderCreatePanel(props: { user: UserResponseDto | null; onError: (m: string) => void }) {
  const { user } = props;

  const [product, setProduct] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [price, setPrice] = useState("10.00");
  const [busy, setBusy] = useState(false);

  const [orders, setOrders] = useState<OrderResponseDto[]>([]);
  const [loading, setLoading] = useState(false);

  async function refreshOrders() {
    if (!user) return;
    setLoading(true);
    props.onError("");
    try {
      const data = await listOrdersByUser(user.id);
      setOrders(Array.isArray(data) ? data : []);
    } catch (e) {
      props.onError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    setOrders([]);
    if (user) refreshOrders();
  }, [user?.id]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    props.onError("");

    if (!user) {
      props.onError("Select a user from the sidebar first.");
      return;
    }

    setBusy(true);
    try {
      await createOrder({
        userId: user.id,
        product,
        quantity: Number(quantity),
        price: Number(price),
      });

      setProduct("");
      setQuantity("1");
      setPrice("10.00");
      await refreshOrders();
    } catch (err) {
      props.onError((err as Error).message ?? "Failed to create order");
    } finally {
      setBusy(false);
    }
  }

  function formatDateTime(iso: string) {
    const d = new Date(iso);

    const dd = String(d.getDate()).padStart(2, "0");
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const yyyy = d.getFullYear();

    const hh = String(d.getHours()).padStart(2, "0");
    const min = String(d.getMinutes()).padStart(2, "0");

    return `${dd}/${mm}/${yyyy}, ${hh}:${min}`;
  }

  if (!user) {
    return (
      <div className="card">
        <div className="cardTitle">No user selected</div>
        <div className="muted">Pick a user from the sidebar to create orders.</div>
      </div>
    );
  }

  return (
    <div className="ordersGrid">
      <div className="card">
        <form onSubmit={submit} className="form">
          <label className="label">
            Product
            <input className="input" value={product} onChange={e => setProduct(e.target.value)} disabled={busy} />
          </label>

          <div className="row">
            <label className="label">
              Quantity
              <input className="input" value={quantity} onChange={e => setQuantity(e.target.value)} disabled={busy} />
            </label>

            <label className="label">
              Price
              <input className="input" value={price} onChange={e => setPrice(e.target.value)} disabled={busy} />
            </label>
          </div>

          <button className="btnPrimary" disabled={busy}>
            {busy ? "Creating..." : "Create Order"}
          </button>
        </form>
      </div>

      <div className="card">
        <div className="rowSpace">
          <div className="cardTitle" style={{ marginBottom: 0 }}>Orders</div>
        </div>

        {orders.length === 0 && !loading ? (
          <div className="muted">No orders yet.</div>
        ) : (
          <div className="table">
            <div className="tableHeader">
              <div>ID</div><div>Product</div><div>Qty</div><div>Price</div><div>Created</div>
            </div>
            {orders.map(o => (
              <div className="tableRow" key={o.id}>
                <div className="mono" title={o.id}>{o.id.slice(0, 8)}…</div>
                <div>{o.product.length > 20 ? o.product.slice(0, 20) + "..." : o.product}</div>
                <div>{o.quantity.toString().length > 20 ? o.quantity.toString().slice(0, 20) + "..." : o.quantity}</div>
                <div>{o.price}</div>
                <div className="mono">{formatDateTime(o.createdAtUtc)}</div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
