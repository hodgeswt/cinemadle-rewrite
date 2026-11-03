import type { Ctor } from './datatype.ts';
import { type ValidationErr, Validate } from "./validations.ts";
import { ok, err, type Result } from '../../result.ts';

export function activate<T extends Ctor>(ctor: T, obj: unknown): Result<InstanceType<T>> {
  const errs: ValidationErr[] = [];
  if (!Validate(ctor, obj, (e) => errs.push(e))) {
    return err(errs.join(','));
  }

  return ok(obj as InstanceType<T>);
}