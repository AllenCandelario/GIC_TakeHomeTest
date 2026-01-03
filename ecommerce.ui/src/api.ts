const USER_BASE = import.meta.env.VITE_USER_API_BASE_URL ?? "http://localhost:5001";
const ORDER_BASE = import.meta.env.VITE_ORDER_API_BASE_URL ?? "http://localhost:5002";

export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
};

export type UserResponseDto = {
  id: string;
  name: string;
  email: string;
  createdAtUtc: string;
};

export type OrderResponseDto = {
  id: string;
  userId: string;
  product: string;
  quantity: number;
  price: number;
  createdAtUtc: string;
};

async function httpJson<T>(url: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
  });

  const contentType = res.headers.get("content-type") ?? "";
  const body = contentType.includes("application/json") || contentType.includes("application/problem+json")
    ? await res.json()
    : await res.text();

  if (!res.ok) {
    const pd = (typeof body === "object" && body) ? (body as ProblemDetails) : null;
    const msg = pd?.detail ?? (typeof body === "string" ? body : `Request failed (${res.status})`);
    throw new Error(msg);
  }

  return body as T;
}

export function listUsers() {
  return httpJson<UserResponseDto[]>(`${USER_BASE}/api/v1/users`, { method: "GET" });
}

export function createUser(payload: { name: string; email: string }) {
  return httpJson<UserResponseDto>(`${USER_BASE}/api/v1/users`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function listOrdersByUser(userId: string) {
  return httpJson<OrderResponseDto[]>(`${ORDER_BASE}/api/v1/orders/user/${userId}`, { method: "GET" });
}

export function createOrder(payload: { userId: string; product: string; quantity: number; price: number }) {
  return httpJson<OrderResponseDto>(`${ORDER_BASE}/api/v1/orders`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}