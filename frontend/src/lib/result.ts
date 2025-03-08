export type Result<T> = {
  data: T | null;
  error: string | null;
  ok: boolean;
};

export function ok<T>(data: T): Result<T> {
  return {
    data: data,
    error: null,
    ok: true,
  } as Result<T>;
}

export function err<T>(error: string): Result<T> {
  return {
    data: null,
    error: error,
    ok: false,
  } as Result<T>;
}

export function match<T>(result: Result<T>, ok: () => void, err: () => void) {
  if (result.ok) {
    ok();
    return;
  }

  err();
}
