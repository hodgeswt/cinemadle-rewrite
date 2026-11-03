import type { TPrimitive, Ctor } from "./datatype.ts";

const mapOfMetaKey = Symbol('mapOf');

type MapOfMap = Map<string | symbol, MapOfOpts>;

/**
 * Options to configure the MapOf decorator
 */
export interface MapOfOpts {
  /** TKey; if complex, then it must be a type with `@validate()` */
  keyDataType: TPrimitive | Ctor

  /** TValue; if complex, then value must be a type with `@validate()` */
  valueDataType: TPrimitive | Ctor

  /** Is this property required for a valid object? */
  required?: boolean

  /** Is this field a list of this type? */
  isList?: boolean

  /** Is the value a list of its primitive? */
  isValueList?: boolean
}

/**
 * Designates that this field is a Map<TKey, TValue>
 * object.
 * @param opts Determines the TKey, TValue and other properties
 * @returns decorator function
 */
export function MapOf(opts: MapOfOpts) {
  return function <This, Value>(
    _value: undefined,
    context: ClassFieldDecoratorContext<This, Value>
  ) {
    const meta = context.metadata as Record<PropertyKey, unknown>;

    let d: MapOfMap;
    if (Object.hasOwn(meta, mapOfMetaKey)) {
      d = meta[mapOfMetaKey] as MapOfMap;
    } else {
      d = new Map();
      meta[mapOfMetaKey] = d;
    }

    d.set(context.name, opts);
  }
}

