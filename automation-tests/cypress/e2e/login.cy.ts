describe('login page', () => {
  it('renders the whole form', () => {
    cy.visit('/index.html');

    cy.getByDataTestId('login-page-link').click();

    cy.getByDataTestId('page-title').should('have.text', 'log in');
    cy.getByDataTestId('email-input').should('exist');
    cy.getByDataTestId('password-input').should('exist');
    cy.getByDataTestId('login-button').should('exist');
  })
})