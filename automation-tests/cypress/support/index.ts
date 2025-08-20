declare namespace Cypress {
  interface Chainable {
    getByDataTestId(dataTestId: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Chainable<JQuery<HTMLElement>>;
    maybeGet(selector: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Chainable<JQuery<HTMLElement>>;
    maybeGetByDataTestId(dataTestId: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Chainable<JQuery<HTMLElement>>;
    customTask(task: string): Cypress.Chainable<Chai.Assertion>;
    init(): void;
  }
}