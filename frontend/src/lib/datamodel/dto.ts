import { hasValue } from "$lib/util.ts";
import { DataType } from "$lib/business/typing/datatype.ts";
import { Validate } from "$lib/business/typing/validations.ts";

export class SignUpErrorDto {
  @DataType('string', { isMap: true, isMapList: true })
  errors!: Map<string, string[]>;
};

export class ImageDto {
  @DataType('string')
  imageData!: string;
}

export class LoginDto {
  @DataType('string')
  tokenType!: string

  @DataType('string')
  accessToken!: string

  @DataType('string')
  refreshToken!: string

  @DataType('number')
  expiresIn!: number
}

export class Hints {
  @DataType('string', { optional: true })
  min?: string

  @DataType('string', { optional: true })
  max?: string

  @DataType('string', { optional: true, isList: true })
  knownValues?: string[]

  @DataType('string', { optional: true, isList: true })
  possibleValues?: string[]
}


export class Field {
  @DataType('string')
  color!: string;

  @DataType('string', {isList: true})
  values!: string[];

  @DataType("number")
  direction!: number;

  @DataType('string', {isMap: true, isMapList: true })
  modifiers!: Map<string, string[]>

  @DataType(Hints, { optional: true })
  hints?: Hints;
}

export class GuessDto {
  @DataType(Field, { isMap: true })
  fields!: Map<string, Field>;

  @DataType('string')
  title!: string;
}

export class PersonDto {
  @DataType('string')
  name!: string;

  @DataType('string')
  role!: string;
}

export class MediaDto {
  @DataType('string')
  title!: string;

  @DataType('number')
  id!: number;

  @DataType(PersonDto, { isList: true })
  cast!: PersonDto[];

  @DataType('string', { isList: true }) 
  genres!: string[];

  @DataType('number')
  year!: number;

  @DataType('string')
  rating!: string;

  @DataType(PersonDto, { isList: true })
  creatives!: PersonDto[];

  @DataType('number')
  boxOffice!: number;
}

export class GameSummaryDto {
  @DataType('string', { isList: true })
  summary!: string[];
}

export class FeatureFlagsDto {
   @DataType('boolean', { isMap: true })
   featureFlags!: Map<string, boolean>;
}

export class CustomGameCreateDto {
  @DataType('number')
  id!: number;
}

export class CustomGameDto {
  @DataType('string')
  id!: string;

  @DataType('number')
  targetMovieId!: number;
}

export class PossibleMediaDto {
  @DataType('number', { isMap: true, isSelf: true })
  self!: Map<string, number>;
}

export class HintsResponseDto {
  @DataType(Hints, { isMap: true, isSelf: true })
  self!: Map<string, Hints>;
}