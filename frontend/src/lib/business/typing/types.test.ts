import { DataType } from "./datatype.ts";

export class TestBool {
  @DataType({ dataType: 'boolean', required: true })
  someBool: boolean = false;
}

export class TestBoolNoReq {
  @DataType({ dataType: 'boolean' })
  someBool?: boolean | undefined = false;

  @DataType({ dataType: 'boolean', isList: true })
  someBoolList?: boolean[] | undefined = [];
}

export class TestString {
  @DataType({ dataType: 'string', required: true })
  someString: string = "";
}

export class TestStringNoReq {
  @DataType({ dataType: 'string' })
  someString?: string | undefined = "";

  @DataType({ dataType: 'string', isList: true })
  someStringList?: string[] | undefined = [];
}

export class TestNumber {
  @DataType({ dataType: 'number', required: true })
  someNumber: number = 0;
}

export class TestNumberNoReq {
  @DataType({ dataType: 'number' })
  someNumber?: number | undefined = 0;

  @DataType({ dataType: 'number', isList: true })
  someNumberList?: number[] | undefined = [];
}

export class TestComplex {
  @DataType({ dataType: TestNumber, required: true })
  some: TestNumber = new TestNumber();
}

export class TestComplexNoReq {
  @DataType({ dataType: TestNumber })
  some?: TestNumber | undefined = new TestNumber();

  @DataType({ dataType: TestNumber, isList: true })
  someList?: TestNumber[] | undefined = [];
}

export class TestMap {
  @DataType({ dataType: TestNumber, required: true, isMap: true })
  some: Record<string, TestNumber> = {};
}

export class TestMapNoReq {
  @DataType({ dataType: TestNumber, isMap: true })
  some: Record<string, TestNumber> = {};

  @DataType({ dataType: 'number', isMap: true })
  somePrimitive?: Record<string, number> | undefined =  {};

  @DataType({ dataType: TestNumber, isList: true, isMap: true })
  someList?: Record<string, TestNumber>[] | undefined = [];
}

export class TestMapList {
  @DataType({ dataType: TestNumber, required: true, isMap: true, isMapList: true })
  some: Record<string, TestNumber[]> = {};
}

export class TestMapListNoReq {
  @DataType({ dataType: TestNumber, isMap: true, isMapList: true })
  some: Record<string, TestNumber[]> = {};

  @DataType({ dataType: 'number', isMap: true, isMapList: true})
  somePrimitive?: Record<string, number[]> | undefined =  {};

  @DataType({ dataType: TestNumber, isList: true, isMap: true, isMapList: true})
  someList?: Record<string, TestNumber[]>[] | undefined = [];
}