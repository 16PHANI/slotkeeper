const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, options: RequestInit = {}, token?: string | null): Promise<T> {
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers ?? {}),
  };

  if (token) {
    (headers as Record<string, string>).Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ detail: response.statusText }));
    throw new ApiError(response.status, problem.detail ?? "Request failed.");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  get: <T,>(path: string, token?: string | null) => request<T>(path, { method: "GET" }, token),
  post: <T,>(path: string, body: unknown, token?: string | null) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
  put: <T,>(path: string, body: unknown, token?: string | null) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }, token),
  delete: <T,>(path: string, token?: string | null) => request<T>(path, { method: "DELETE" }, token),
};
