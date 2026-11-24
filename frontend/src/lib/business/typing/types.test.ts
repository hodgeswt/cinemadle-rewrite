import { DataType } from "./datatype.ts";

export class TestBool {
  @DataType('boolean')
  someBool!: boolean;
}

export class TestSelf {
  @DataType('boolean', { isSelf: true })
  self!: boolean;
}

export class TestBoolNoReq {
  @DataType('boolean', { optional: true })
  someBool?: boolean | undefined;

  @DataType('boolean', { isList: true, optional: true })
  someBoolList?: boolean[] | undefined = [];
}

export class TestString {
  @DataType('string')
  someString!: string;
}

export class TestStringNoReq {
  @DataType('string', { optional: true })
  someString?: string | undefined;

  @DataType('string', { isList: true, optional: true })
  someStringList?: string[] | undefined;
}

export class TestNumber {
  @DataType('number')
  someNumber!: number;
}

export class TestNumberNoReq {
  @DataType('number', { optional: true })
  someNumber?: number | undefined;

  @DataType('number', { isList: true, optional: true })
  someNumberList?: number[] | undefined;
}

export class TestComplex {
  @DataType(TestNumber)
  some!: TestNumber;
}

export class TestComplexNoReq {
  @DataType(TestNumber, { optional: true })
  some?: TestNumber | undefined;

  @DataType(TestNumber, { isList: true, optional: true })
  someList?: TestNumber[] | undefined;
}

export class TestMap {
  @DataType(TestNumber, { isMap: true })
  some!: Record<string, TestNumber>;
}

export class TestMapNoReq {
  @DataType(TestNumber, { isMap: true, optional: true })
  some!: Record<string, TestNumber>;

  @DataType('number', { isMap: true, optional: true })
  somePrimitive?: Record<string, number> | undefined;

  @DataType(TestNumber, { isList: true, isMap: true, optional: true })
  someList?: Record<string, TestNumber>[] | undefined;
}

export class TestMapList {
  @DataType(TestNumber, { isMap: true, isMapList: true })
  some!: Record<string, TestNumber[]>;
}

export class TestMapListNoReq {
  @DataType(TestNumber, { isMap: true, isMapList: true, optional: true })
  some!: Record<string, TestNumber[]>;

  @DataType('number', { isMap: true, isMapList: true, optional: true })
  somePrimitive?: Record<string, number[]> | undefined;

  @DataType(TestNumber, { isList: true, isMap: true, isMapList: true, optional: true })
  someList?: Record<string, TestNumber[]>[] | undefined;
}