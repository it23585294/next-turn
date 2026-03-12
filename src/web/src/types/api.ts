/**
 * Shared API response shape types.
 * Mirrors the ASP.NET Core ProblemDetails format produced by the middleware.
 */

/** Standard Problem Details (RFC 7807) returned by the API on errors */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** FluentValidation errors: field → [messages] */
  errors?: Record<string, string[]>;
}

/** Parsed error from an Axios error response */
export interface ApiError {
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  raw: ProblemDetails;
}
