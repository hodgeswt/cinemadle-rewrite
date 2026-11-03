import { DataType } from "./datatype.ts";
import { Validate } from "./validations.ts";
import { describe, it, beforeEach } from "node:test";
import { type ValidationErr } from './validations.ts';
import assert from 'node:assert';
import { activate } from "./activator.ts";

class TestBool {
  @DataType({ dataType: 'boolean', required: true })
  someBool: boolean = false;
}

class TestBoolNoReq {
  @DataType({ dataType: 'boolean' })
  someBool: boolean = false;

  @DataType({ dataType: 'boolean', isList: true })
  someBoolList: boolean[] = [];
}

class TestString {
  @DataType({ dataType: 'string', required: true })
  someString: string = "";
}

class TestStringNoReq {
  @DataType({ dataType: 'string' })
  someString: string = "";

  @DataType({ dataType: 'string', isList: true })
  someStringList: string[] = [];
}

class TestNumber {
  @DataType({ dataType: 'number', required: true })
  someNumber: number = 0;
}

class TestNumberNoReq {
  @DataType({ dataType: 'number' })
  someNumber: number = 0;

  @DataType({ dataType: 'number', isList: true })
  someNumberList: number[] = [];
}

class TestComplex {
  @DataType({ dataType: TestNumber, required: true })
  some: TestNumber = new TestNumber();
}

class TestComplexNoReq {
  @DataType({ dataType: TestNumber })
  some: TestNumber = new TestNumber();

  @DataType({ dataType: TestNumber, isList: true })
  someList: TestNumber[] = [];
}

class TestMap {
  @DataType({ dataType: TestNumber, required: true, isMap: true })
  some: Map<string, TestNumber> = new Map<string, TestNumber>();
}

class TestMapNoReq {
  @DataType({ dataType: TestNumber, isMap: true })
  some: Map<string, TestNumber> = new Map<string, TestNumber>();

  @DataType({ dataType: 'number', isMap: true })
  somePrimitive: Map<string, number> = new Map<string, number>();

  @DataType({ dataType: TestNumber, isList: true, isMap: true })
  someList: Map<string, TestNumber>[] = [];
}

describe("primitive activation", () => {
  describe("of a valid", () => {
    it("boolean", () => {
      const x = { someBool: true };
      assert.strictEqual((activate(TestBool, x) as unknown as TestBool).someBool, x.someBool);
    });

    it("string", () => {
      const x = { someString: "asdf" };
      assert.strictEqual((activate(TestString, x) as unknown as TestString).someString, x.someString);
    });

    it("number", () => {
      const x = { someNumber: 1235 };
      assert.strictEqual((activate(TestNumber, x) as unknown as TestNumber).someNumber, x.someNumber);
    });
  });

  describe("of an invalid", () => {
    it("boolean", () => {
      const x = { someBool: "asdf" };
      assert.strictEqual((activate(TestBool, x) as ValidationErr[]).length, 1);
    });

    it("string", () => {
      const x = { someString: 1234 };
      assert.strictEqual((activate(TestString, x) as ValidationErr[]).length, 1);
    });

    it("number", () => {
      const x = { someNumber: "asdf" };
      assert.strictEqual((activate(TestNumber, x) as ValidationErr[]).length, 1);
    });
  });

  describe("of an absent", () => {
    it("boolean", () => {
      assert.strictEqual((activate(TestBool, {}) as ValidationErr[]).length, 1);
    });

    it("string", () => {
      assert.strictEqual((activate(TestString, {}) as ValidationErr[]).length, 1);
    });

    it("number", () => {
      assert.strictEqual((activate(TestNumber, {}) as ValidationErr[]).length, 1);
    });
  });
});