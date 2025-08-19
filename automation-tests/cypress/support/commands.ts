Cypress.Commands.add('getByDataTestId', (dataTestId: string): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.get(`[data-testid="${dataTestId}"]`);
})

export const destroyDatabase = async () => {
    const response = await fetch(`${Cypress.config().baseUrl}/api/cinemadle/destroy`, { method: 'DELETE' });
    if (!response.ok) {
        throw new Error('unable to destroy database');
    }
}

export const goToPage = (page: string) => {
    cy.getByDataTestId('menu-button').click();
    cy.getByDataTestId(`${page.replaceAll(' ', '')}-link`).click();

    if (page !== 'home') {
        cy.getByDataTestId('page-title').should('have.text', page);
    }
}