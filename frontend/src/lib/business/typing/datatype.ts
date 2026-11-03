export type TString = 'string';
export type TNumber = 'number';
export type TBoolean = 'boolean';
export type TPrimitive = TString | TNumber | TBoolean;

// deno-lint-ignore no-explicit-any
export type Ctor = abstract new (...args: any[]) => object

export function isTPrimitive(obj: unknown): obj is TPrimitive {
  return (obj === 'date' || obj === 'string' || obj === 'number' || obj === 'boolean');
}

export const dataTypeMetaKey = Symbol('dataType');

type DataTypeMap = Map<string | symbol, DataTypeOpts>;

export interface DataTypeOpts {
  /** Type of field */
  dataType: TPrimitive | Ctor

  /** Is this field required for the object to be valid? */
  required?: boolean

  /** Is this field a list of this type? */
  isList?: boolean

  /** Is this field a Map<string, this type>? */
  isMap?: boolean

  /** Is this field a Map<string, Array<this type>>? (requires isMap = true) */
  isMapList?: boolean
}

/**
 * Designates this field to be a specified type for validation.
 * If the type is a list, provide the object's type
 * 
 * If the type is complex, it must use the `@validate()` decorator.
 * 
 * @param opts Configures the data type of the field
 * @returns Decorator
 */
export function DataType(opts: DataTypeOpts) {
  return function <This, Value>(
    _value: undefined,
    context: ClassFieldDecoratorContext<This, Value>
  ) {
    const meta = context.metadata as Record<PropertyKey, unknown>;

    let d: DataTypeMap;
    if (Object.hasOwn(meta, dataTypeMetaKey)) {
      d = meta[dataTypeMetaKey] as DataTypeMap;
    } else {
      d = new Map();
      meta[dataTypeMetaKey] = d;
    }

    d.set(context.name, opts);
  }
}