import { goToPage, logIn } from "../support/commands";

describe('login page', () => {
  before(() => {
    cy.customTask('destroyDatabase');
  });

  afterEach(() => {
    cy.customTask('destroyDatabase');
  });

  it('renders the whole form', () => {
    cy.visit('/index.html');

    cy.getByDataTestId('login-page-link').click();

    cy.getByDataTestId('page-title').should('have.text', 'log in');
    cy.getByDataTestId('email-input').should('exist');
    cy.getByDataTestId('password-input').should('exist');
    cy.getByDataTestId('login-button').should('exist');
  })

  it('allows login', () => {
    cy.visit('/index.html');

    const { username } = logIn({initialize: true});

    cy.getByDataTestId('cinemadle-date').should('exist');

    goToPage('about');

    cy.getByDataTestId('user-email').should('have.text', `User: ${username}`)
  })

  it('detects invalid login', () => {
    cy.visit('/index.html');

    goToPage('sign up');

    const username: string = 'asdf@asdf.com';
    const password: string = 'Password1$';

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('passwordconfirm-input').type(password);
    cy.getByDataTestId('signup-button').click();

    cy.getByDataTestId('page-title').should('have.text', 'log in');

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type('error');
    cy.getByDataTestId('login-button').click();


    cy.getByDataTestId('error-title-text').should('have.text', 'uh-oh!');
    cy.getByDataTestId('error-body-text').should('have.text', `Unable to log in`);
  })
})