declare namespace Cypress {
  interface Chainable {
    getByDataTestId(dataTestId: string): Chainable<JQuery<HTMLElement>>;
    maybeGet(selector: string): Chainable<JQuery<HTMLElement>>;
    maybeGetByDataTestId(dataTestId: string): Chainable<JQuery<HTMLElement>>;
    customTask(task: string): Cypress.Chainable<Chai.Assertion>;
  }
}