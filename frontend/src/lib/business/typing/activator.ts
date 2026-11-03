import type { Ctor } from './datatype.ts';
import { type ValidationErr, Validate } from "./validations.ts";

export function activate<T extends Ctor>(ctor: T, obj: unknown): ValidationErr[] | T {
  const errs: ValidationErr[] = [];
  if (!Validate(ctor, obj, (e) => errs.push(e))) {
    return errs;
  }

  return obj as T;
}