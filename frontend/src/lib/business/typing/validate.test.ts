import { Validate } from "./validations.ts";
import { describe, it, beforeEach } from "node:test";
import { type ValidationErr } from './validations.ts';
import { TestBool, TestBoolNoReq, TestString, TestNumber, TestStringNoReq, TestComplex, TestComplexNoReq, TestNumberNoReq, TestMap, TestMapNoReq } from "./types.test.ts";
import assert from 'node:assert';


let currentErrs: ValidationErr[] = [];
const errLog = (err: ValidationErr) => currentErrs.push(err);
const errs = () => currentErrs.join(', ');

describe("primitive validation", () => {
  describe("handles a valid", () => {
    beforeEach(() => currentErrs = []);

    describe("omitted", () => {
      beforeEach(() => currentErrs = []);
      it("boolean", () => {
        assert.strictEqual(Validate(TestBoolNoReq, {}, errLog), true, errs());
      });

      it("string", () => {
        assert.strictEqual(Validate(TestStringNoReq, {}, errLog), true, errs());
      });

      it("number", () => {
        assert.strictEqual(Validate(TestNumberNoReq, {}, errLog), true, errs());
      });
    });

    it("boolean", () => {
      const x = {
        someBool: true
      };

      assert.strictEqual(Validate(TestBool, x, errLog), true, errs());
      assert.strictEqual(Validate(TestBoolNoReq, x, errLog), true, errs());
    });

    it("empty boolean list", () => {
      const x = {
        someBoolList: []
      };

      assert.strictEqual(Validate(TestBoolNoReq, x, errLog), true, errs());
    });

    it("boolean list", () => {
      const x = {
        someBoolList: [true, false]
      };

      assert.strictEqual(Validate(TestBoolNoReq, x, errLog), true, errs());
    });

    it("string", () => {
      const x = {
        someString: "asdf"
      };

      assert.strictEqual(Validate(TestString, x, errLog), true, errs());
      assert.strictEqual(Validate(TestStringNoReq, x, errLog), true, errs());
    });

    it("empty string list", () => {
      const x = {
        someStringList: []
      };

      assert.strictEqual(Validate(TestStringNoReq, x, errLog), true, errs());
    });

    it("string list", () => {
      const x = {
        someStringList: ["", "asdf"]
      };

      assert.strictEqual(Validate(TestStringNoReq, x, errLog), true, errs());
    });

    it("number", () => {
      const x = {
        someNumber: 1234
      };

      assert.strictEqual(Validate(TestNumber, x, errLog), true, errs());
      assert.strictEqual(Validate(TestNumberNoReq, x, errLog), true, errs());
    });

    it("empty number list", () => {
      const x = {
        someNumberList: []
      };

      assert.strictEqual(Validate(TestNumberNoReq, x, errLog), true, errs());
    });

    it("number list", () => {
      const x = {
        someNumberList: [1234, 1234]
      };

      assert.strictEqual(Validate(TestNumberNoReq, x, errLog), true, errs());
    });
  })


  describe("handles an invalid", () => {
    beforeEach(() => currentErrs = []);

    describe("omitted", () => {
      beforeEach(() => currentErrs = []);

      it("boolean", () => {
        assert.strictEqual(Validate(TestBool, {}, errLog), false, errs());
      });

      it("string", () => {
        assert.strictEqual(Validate(TestString, {}, errLog), false, errs());
      });

      it("number", () => {
        assert.strictEqual(Validate(TestNumber, {}, errLog), false, errs());
      });
    });

    describe("list of", () => {
      beforeEach(() => currentErrs = []);

      it("booleans", () => {
        assert.strictEqual(
          Validate(TestBoolNoReq, { someBoolList: ["asdf", true, 1234, {}] }, errLog),
          false,
          errs()
        );
      });

      it("strings", () => {
        assert.strictEqual(
          Validate(TestStringNoReq, { someStringList: ["asdf", true, 1234, {}] }, errLog),
          false,
          errs()
        );
      });

      it("numbers", () => {
        assert.strictEqual(
          Validate(TestNumberNoReq, { someNumberList: ["asdf", true, 1234, {}] }, errLog),
          false,
          errs()
        );
      });
    });

    it("boolean", () => {
      [
        ['string', { someBool: "a string" }],
        ['number', { someBool: 123 }],
        ['object', { someBool: {} }],
        ['list', { someBool: [] }],
        ['nothing', {}]
      ].forEach((x) => {
        assert.strictEqual(
          Validate(TestBool, x[1], errLog),
          false,
          `Failed to identify invalid boolean when provided ${typeof x[0]}. Msg: ${errs()}`
        );
      })
    });

    it("string", () => {
      [
        ['boolean', { someString: true }],
        ['number', { someString: 123 }],
        ['object', { someString: {} }],
        ['list', { someString: [] }],
        ['nothing', {}]
      ].forEach((x) => {
        assert.strictEqual(
          Validate(TestString, x[1], errLog),
          false,
          `Failed to identify invalid string when provided ${typeof x[0]}. Msg: ${errs()}`
        );
      })
    });

    it("number", () => {
      [
        ['string', { someNumber: "a string" }],
        ['boolean', { someNumber: true }],
        ['object', { someNumber: {} }],
        ['list', { someNumber: [] }],
        ['nothing', {}]
      ].forEach((x) => {
        assert.strictEqual(
          Validate(TestBool, x[1], errLog),
          false,
          `Failed to identify invalid number when provided ${typeof x[0]}. Msg: ${errs()}`
        );
      })
    });
  })
});

describe("map validation", () => {
  beforeEach(() => currentErrs = []);

  it("handles a valid map", () => {
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

    assert.strictEqual(Validate(TestMap, x, errLog), true, errs());
    assert.strictEqual(Validate(TestMapNoReq, y, errLog), true, errs());
  });

  it("handles an invalid map", () => {
    const x = {
      some: {
        'item1': { someNumber: 'asdf' },
        'item2': { someNumber: 9 }
      }
    };

    const y = {
      somePrimitive: {
        'item1': 'asdf',
        'item2': 10,
      }
    }

    assert.strictEqual(Validate(TestMap, x, errLog), false, errs());
    assert.strictEqual(Validate(TestMapNoReq, y, errLog), false, errs());

  });
});

describe("complex validation handles", () => {
  beforeEach(() => currentErrs = []);

  it("a valid child", () => {
    const x = {
      some: {
        someNumber: 9
      }
    };

    assert.strictEqual(Validate(TestComplex, x, errLog), true, errs());
  });

  it("a valid child list", () => {
    const x = {
      someList: [{
        someNumber: 9
      }]
    };

    assert.strictEqual(Validate(TestComplexNoReq, x, errLog), true, errs());
  });

  it("a valid absent child", () => {
    const x = {};

    assert.strictEqual(Validate(TestComplexNoReq, x, errLog), true, errs());
  });

  it("a child with an empty list", () => {
    const x = { someList: [] };

    assert.strictEqual(Validate(TestComplexNoReq, x, errLog), true, errs());
  });

  it("an invalid child", () => {
    const x = {
      some: {
        someNumber: "asdf"
      }
    };

    assert.strictEqual(Validate(TestComplex, x, errLog), false, errs());
  });

  it("an invalid child list", () => {
    const x = {
      someList: [
        { someNumber: "asdf" },
        { someNumber: 9 }
      ]
    };

    assert.strictEqual(Validate(TestComplexNoReq, x, errLog), false, errs());
  });

  it("an invalid absent child", () => {
    const x = {};

    assert.strictEqual(Validate(TestComplex, x, errLog), false, errs());
  });
});