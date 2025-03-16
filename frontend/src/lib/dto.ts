import { isArray, hasValue } from "$lib/util";

export type PossibleMediaDto = {
    [key: string]: string;
}

export type GuessDto = {
  fields: { [key: string]: Field };
};

export type Field = {
  color: string;
  direction: number;
  values: string[];
};

export function isPossibleMediaDto(obj: any): obj is PossibleMediaDto {
    if (!hasValue(obj)) {
        return false;
    }

    for (const key in obj) {
        if (typeof key !== 'string' || typeof obj[key] !== 'string') {
            return false
        }
    }

    return true
}

export function isGuessDto(obj: any): obj is GuessDto {
  if (!hasValue(obj) || !hasValue(obj.fields)) {
    return false;
  }

  let valid = true;

  for (const key in obj.fields) {
    if (typeof key !== "string") {
      valid = false;
      break;
    }

    if (!isField(obj.fields[key])) {
      valid = false;
      break;
    }
  }

  return valid;
}

export function isField(obj: any): obj is Field {
  return (
    hasValue(obj) &&
    hasValue(obj.color) &&
    typeof obj.color === "string" &&
    hasValue(obj.direction) &&
    typeof obj.direction === "number" &&
    isArray(obj.values, "string")
  );
}
