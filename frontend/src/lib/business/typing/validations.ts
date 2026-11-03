// deno-lint-ignore-file no-explicit-any
import type { Ctor, DataTypeOpts, TPrimitive } from "./datatype.ts";
import { dataTypeMetaKey, isTPrimitive } from './datatype.ts';

type ValidationFunc = (obj: unknown, errCallback?: ValidationErrCallback) => boolean;

export type ValidationErr = string;
export type ValidationErrCallback = (err: ValidationErr) => void;

const noopErr = (_err: ValidationErr) => { };


export function Validate<C extends Ctor>(ctor: C, obj: any, errCallback?: ValidationErrCallback): obj is C {
  const err = errCallback ? errCallback : noopErr;
  if (typeof obj !== "object" || obj === null || obj === undefined) {
    err("expected obj to be non-null");
    return false;
  }

  const objMetadata = (ctor as any)[Symbol.metadata]
  const fieldsToValidate: Map<string, DataTypeOpts> = objMetadata?.[dataTypeMetaKey] ?? [];

  let ok = true;

  for (const [k, v] of fieldsToValidate.entries()) {
    if (!(k in obj)) {
      if (v.required) {
        err(`Expected absent property ${k} to be present on obj`);
        ok = false;
      }

      continue;
    }

    const val = obj[k];

    let toValidate = [val];
    if (v.isList) {
      if (!Array.isArray(val)) {
        err(`Expected property ${k} on obj to be a list`);
        ok = false;
        continue;
      }

      toValidate = val;
    }

    const errs: ValidationErr[] = [];
    const getAll = (err: ValidationErr) => {
      errs.push(err);
    }

    let invalid;
    if (isTPrimitive(v.dataType)) {
      invalid = (i: any) => !getValidator(v.dataType as TPrimitive)(i, getAll);
    } else {
      invalid = (i: any) => !Validate(v.dataType as Ctor, i, getAll);
    }

    if (!v.isMap) {
      const invalidItems = toValidate.filter(invalid);
      if (invalidItems.length > 0) {
        err(`property ${k} failed validation: ${errs.join(',')}`);
        ok = false;
      }

      continue;
    }

    for (const item of toValidate) {
      if (typeof item !== "object") {
        err(`expected obj to be a map`);
        ok = false;
        continue;
      }

      for (const [mapKey, mapVal] of Object.entries(item)) {
        if (v.isMapList && !Array.isArray(mapVal)) {
          err(`map ${k}'s key ${mapKey} was expected to be a list of Map<string, T>`);
          ok = false;
          continue;
        }

        const items = v.isMapList ? mapVal as any[] : [ mapVal ];
        const invalidItems = items.filter(invalid);
        
        if (invalidItems.length > 0) {
          err(`map ${k}'s key ${mapKey} failed validation: ${errs.join(',')}`);
          ok = false;
        }
      }
    }
  }

  return ok;
}

function getValidator(type: TPrimitive): ValidationFunc {
  switch (type) {
    case 'string':
      return validateString
    case 'boolean':
      return validateBoolean
    case 'number':
      return validateNumber
    default:
      return (_: unknown) => false
  }
}

function notNull(obj: unknown): boolean {
  return obj !== undefined && obj !== null;
}

function validateString(obj: unknown, errCallback?: ValidationErrCallback): obj is string {
  const x = notNull(obj) && typeof obj === 'string';

  if (!x && errCallback) {
    errCallback(`expected obj ${obj} to be a string`);
  }

  return x;
}

function validateBoolean(obj: unknown, errCallback?: ValidationErrCallback): obj is boolean {
  const x = notNull(obj) && typeof obj === 'boolean';
  if (!x && errCallback) {
    errCallback(`expected obj ${obj} to be a boolean`);
  }

  return x;
}

function validateNumber(obj: unknown, errCallback?: ValidationErrCallback): obj is number {
  const x = notNull(obj) && typeof obj === 'number';
  if (!x && errCallback) {
    errCallback(`expected obj ${obj} to be a number`);
  }

  return x;
}