import { describe, it } from "node:test";
import assert from 'node:assert';
import { activate } from "./activator.ts";
import { TestMap, TestMapNoReq, TestBool, TestBoolNoReq, TestString, TestNumber, TestStringNoReq, TestNumberNoReq, TestComplex, TestComplexNoReq } from "./types.test.ts";

describe("activation", () => {
  describe("of a valid", () => {
    it("boolean", () => {
      const x = { someBool: true };
      const r = activate(TestBool, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someBool, x.someBool);
    });

    it("missing boolean", () => {
      const x = {};
      const r = activate(TestBoolNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someBool, undefined);
    });

    it("string", () => {
      const x = { someString: "asdf" };
      const r = activate(TestString, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someString, x.someString);
    });

    it("missing string", () => {
      const x = {};
      const r = activate(TestStringNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someString, undefined);
    });

    it("number", () => {
      const x = { someNumber: 1235 };
      const r = activate(TestNumber, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someNumber, x.someNumber);
    });

    it("missing number", () => {
      const x = {};
      const r = activate(TestNumberNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someNumber, undefined);
    });

    it("complex type", () => {
      const x = { some: { someNumber: 1234 } };
      const r = activate(TestComplex, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.some.someNumber, x.some.someNumber);
    });

    it("missing complex type", () => {
      const x = {};
      const r = activate(TestComplexNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.some?.someNumber, undefined);
    });

    it("list", () => {
      const x = { someBoolList: [true, false] };
      const r = activate(TestBoolNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someBoolList, x.someBoolList);
    });

    it("complex list", () => {
      const x = { someList: [{ someNumber: 999 }] };
      const r = activate(TestComplexNoReq, x);

      assert.strictEqual(r.ok, true);
      assert.strictEqual(r.data!.someList, x.someList);
    });

    it("map", () => {
      const x = {
        some: {
          'item1': { someNumber: 9 },
          'item2': { someNumber: 9 }
        }
      };

      const y = {
        somePrimitive: {
          'item1': 9,
          'item2': 10,
        }
      }

      const r1 = activate(TestMap, x);
      const r2 = activate(TestMapNoReq, y);

      assert.strictEqual(r1.ok, true);
      assert.strictEqual(r2.ok, true);

      for (const [key, value] of Object.entries(x.some)) {
        assert.strictEqual(r1.data!.some[key], value);
      }

      for (const [key, value] of Object.entries(y.somePrimitive)) {
        assert.strictEqual(r2.data!.somePrimitive![key], value);
      }
    });
  });

  describe("of an invalid", () => {
    it("boolean", () => {
      const x = { someBool: "asdf" };
      const r = activate(TestBool, x);

      assert.strictEqual(r.ok, false)
    });

    it("missing boolean", () => {
      const x = {};
      const r = activate(TestBool, x);

      assert.strictEqual(r.ok, false);
    });

    it("string", () => {
      const x = { someString: 1234 };
      const r = activate(TestString, x);

      assert.strictEqual(r.ok, false)
    });

    it("missing string", () => {
      const x = {};
      const r = activate(TestString, x);

      assert.strictEqual(r.ok, false);
    });

    it("number", () => {
      const x = { someNumber: "asdf" };
      const r = activate(TestNumber, x);

      assert.strictEqual(r.ok, false);
    });

    it("missing number", () => {
      const x = {};
      const r = activate(TestNumber, x);

      assert.strictEqual(r.ok, false);
    });

    it("complex type", () => {
      const x = { some: { someNumber: "asdf" } };
      const r = activate(TestComplex, x);

      assert.strictEqual(r.ok, false);
    });

    it("missing complex type", () => {
      const x = {};
      const r = activate(TestComplex, x);

      assert.strictEqual(r.ok, false);
    });

    it("list", () => {
      const x = { someBoolList: [true, "999"] };
      const r = activate(TestBoolNoReq, x);

      assert.strictEqual(r.ok, false);
    });

    it("complex list", () => {
      const x = { someList: [{ someNumber: true }] };
      const r = activate(TestComplexNoReq, x);

      assert.strictEqual(r.ok, false);
    });
  });
});