// deno-lint-ignore-file no-explicit-any
import type { Ctor, DataTypeOpts, TPrimitive } from "./datatype.ts";
import { dataTypeMetaKey, isTPrimitive } from './datatype.ts';

type ValidationFunc = (obj: unknown, errCallback?: ValidationErrCallback) => boolean;

export type ValidationErr = string;
export type ValidationErrCallback = (err: ValidationErr) => void;

const noopErr = (_err: ValidationErr) => { };

class FieldValidationContext {
  readonly fieldName: string;
  readonly fieldType: DataTypeOpts;
  
  readonly isInvalid: (i: any) => boolean;

  private _valid: boolean;
  private readonly _errors: ValidationErr[];

  constructor(fieldName: string, fieldType: DataTypeOpts) {
    this.fieldName = fieldName;
    this.fieldType = fieldType;
    
    const validator = getValidator(fieldType.dataType);
    this.isInvalid = (x: any) => !validator(x, (e) => this.addError(e));

    this._valid = true;
    this._errors = [];
  }

  public errors(): string {
    return `${this.fieldName} failed validation: ${this._errors.join(', ')}`;
  }

  public addError(err: ValidationErr): void {
    this._errors.push(err);
    this._valid = false;
  }

  public isValid(): boolean {
    return this._valid;
  }

}

function ValidateMap(toValidate: any[], ctx: FieldValidationContext): void {
  for (const item of toValidate) {
    if (typeof item !== "object") {
      ctx.addError(`expected obj to be a map`);
      continue;
    }

    for (const [mapKey, mapVal] of Object.entries(item)) {
      if (ctx.fieldType.isMapList && !Array.isArray(mapVal)) {
        ctx.addError(`map key ${mapKey} was expected to be a list of Map<string, T>`);
        continue;
      }

      const items = ctx.fieldType.isMapList ? mapVal as any[] : [mapVal];
      const invalidItems = items.filter(ctx.isInvalid);

      if (invalidItems.length > 0) {
        ctx.addError(`map key ${mapKey} failed validation: ${ctx.errors()}`);
        continue;
      }
    }
  }
}

function ValidateField(val: any, ctx: FieldValidationContext): void {
  let toValidate = [val];
  if (ctx.fieldType.isList) {
    if (!Array.isArray(val)) {
      ctx.addError('Expected property to be a list');
      return;
    }

    toValidate = val;
  }

  if (ctx.fieldType.isMap) {
    ValidateMap(toValidate, ctx);
    return;
  }

  toValidate.forEach((x, i) => {
    if (ctx.isInvalid(x)) {
      ctx.addError(`item ${i} failed validation`);
    }
  });
}

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
    const ctx = new FieldValidationContext(k, v);

    if (!(k in obj)) {
      if (v.required) {
        ctx.addError(`Absent required property ${k}`);
      }
    } else {
      ValidateField(obj[k], ctx);
    }

    if (!ctx.isValid()) {
      err(ctx.errors());
    }

    ok = ok && ctx.isValid();
  }

  return ok;
}

function getValidator(dataType: Ctor | TPrimitive): ValidationFunc {
  let validator: ValidationFunc;
  if (isTPrimitive(dataType)) {
    validator = (i: any): boolean => getPrimitiveValidator(dataType as TPrimitive)(i);
  } else {
    validator = (i: any): boolean => Validate(dataType as Ctor, i);
  }

  return validator;
}

function getPrimitiveValidator(type: TPrimitive): ValidationFunc {
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