import { dataTypeMetaKey, type Ctor, type InternalDataTypeOpts } from './datatype.ts';
import { type ValidationErr, Validate } from "./validations.ts";
import { ok, err, type Result } from '../../result.ts';

export function activate<T extends Ctor>(ctor: T, obj: unknown): Result<InstanceType<T>> {
  const errs: ValidationErr[] = [];
  if (!Validate(ctor, obj, (e) => errs.push(e))) {
    return err(errs.join(','));
  }

  // deno-lint-ignore no-explicit-any
  const objMetadata = (ctor as any)[Symbol.metadata]
  const fieldsToValidate: Map<string, InternalDataTypeOpts> = objMetadata?.[dataTypeMetaKey] ?? [];

  const selfProp = fieldsToValidate.entries().find(([_, v]) => v.isSelf === true);
  if (selfProp) {
    const [k, _] = selfProp;
    const o: Record<string, unknown> = {}
    o[k] = obj;

    return ok(o as InstanceType<T>);
  }

  return ok(obj as InstanceType<T>);
}