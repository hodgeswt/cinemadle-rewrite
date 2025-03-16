import { isGuessDto } from "$lib/dto";
import type { CardDomain, GuessDomain } from "$lib/domain";
import { err, ok, type Result } from "$lib/result";

export function GuessDtoToDomain(guess: any): Result<GuessDomain> {
  if (!isGuessDto(guess)) {
    return err("Invalid data type");
  }

  let out = {
    title: "Unknown",
    cards: [],
  } as GuessDomain;

  for (const k in guess.fields) {
    const v = guess.fields[k];

    let card = {
      color: v.color === "grey" ? "gray" : v.color,
      data: v.values,
    } as CardDomain;

    out.cards.push(card);
  }

  return ok(out);
}
