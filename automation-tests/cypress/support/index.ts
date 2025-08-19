declare namespace Cypress {
  interface Chainable {
    getByDataTestId(dataTestId: string): Chainable<JQuery<HTMLElement>>;
  }
}