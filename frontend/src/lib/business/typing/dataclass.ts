// deno-lint-ignore-file no-explicit-any
const argFields = new WeakMap<object, PropertyKey[]>();

export function DataClass() {
  return <T extends new (...args: any[]) => object>(Base: T): T => {
    const fields = argFields.get(Base.prototype) ?? [];

    return class extends Base {
      constructor(...args: any) {
        super(...args);

        fields.forEach((field, index) => {
          Reflect.set(this, field, args[index]);
        });
      }
    } as T;
  };
}