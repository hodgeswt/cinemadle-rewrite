Cypress.Commands.add('getByDataTestId', (dataTestId: string): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.get(`[data-testid="${dataTestId}"]`);
})

