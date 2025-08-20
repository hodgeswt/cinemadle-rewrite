import { goToPage } from "../support/commands";

describe('login page', () => {
  before(() => {
    cy.customTask('destroyDatabase');
  });

  afterEach(async () => {
    cy.customTask('destroyDatabase');
  });

  it('renders the whole form', () => {
    cy.visit('/index.html');

    goToPage('sign up');

    cy.getByDataTestId('email-input').should('exist');
    cy.getByDataTestId('password-input').should('exist');
    cy.getByDataTestId('passwordconfirm-input').should('exist');
    cy.getByDataTestId('signup-button').should('exist');
  })

  it('allows signup', () => {
    cy.visit('/index.html');

    goToPage('sign up');

    const username: string = 'asdf@asdf.com';
    const password: string = 'Password1$';

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('passwordconfirm-input').type(password);
    cy.getByDataTestId('signup-button').click();

    cy.getByDataTestId('page-title').should('have.text', 'log in');
  });

  it('does not allow duplicate signup', () => {
    cy.visit('/index.html');

    goToPage('sign up');

    const username: string = 'asdf@asdf.com';
    const password: string = 'Password1$';

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('passwordconfirm-input').type(password);
    cy.getByDataTestId('signup-button').click();

    cy.getByDataTestId('page-title').should('have.text', 'log in');

    goToPage('sign up');

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('passwordconfirm-input').type(password);
    cy.getByDataTestId('signup-button').click();

    cy.getByDataTestId('error-title-text').should('have.text', 'uh-oh!');
    cy.getByDataTestId('error-body-text').should('have.text', `Username '${username}' is already taken.`);
  });

  it('detects password mismatch', () => {
    cy.visit('/index.html');

    goToPage('sign up');

    const username: string = 'asdf@asdf.com';
    const password: string = 'Password1$';

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('passwordconfirm-input').type('error');
    cy.getByDataTestId('signup-button').click();

    cy.getByDataTestId('error-title-text').should('have.text', 'uh-oh!');
    cy.getByDataTestId('error-body-text').should('have.text', `Passwords do not match`);
  });
});
