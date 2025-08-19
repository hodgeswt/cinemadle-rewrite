import { destroyDatabase, isoDateNoTime, logIn } from "../support/commands";

describe('home page', () => {
    before(() => {
        destroyDatabase();
    });

    beforeEach(() => {
        cy.visit('/index.html');
    })

    afterEach(async () => {
        destroyDatabase();
    });

    describe('logged out', () => {
        it('should have the date', () => {
            cy.getByDataTestId('cinemadle-date').should('have.text', isoDateNoTime());
        });

        it('should have log in link', () => {
            cy.getByDataTestId('login-page-link').should('exist');
            cy.getByDataTestId('login-page-link').should('have.text', 'log in');
        });

        it('should render a guess', () => {
            cy.getByDataTestId('guess-input').type('Shrek 2');
            cy.getByDataTestId('submit-button').click();
            
            cy.getByDataTestId('guess-title').should('have.text', 'Shrek 2');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            logIn({initialize: true});
        })
    });

});