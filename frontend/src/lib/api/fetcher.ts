export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  type?: string;
}

export class ApiError extends Error {
  status: number;
  detail: string;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `Error HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.detail = problem.detail ?? problem.title ?? `Error ${status}`;
  }
}

export async function fetcher<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json", ...options?.headers },
    ...options,
  });

  if (!res.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = await res.json();
    } catch {}
    throw new ApiError(res.status, problem);
  }

  const text = await res.text();
  if (!text) return null as T;
  return JSON.parse(text) as T;
}
