import { goToPage, logIn } from "../support/commands";

describe('about page', () => {
    before(() => {
        cy.customTask('destroyDatabase');
    });

    beforeEach(() => {
        cy.visit('/index.html');
    })

    afterEach(() => {
        cy.customTask('destroyDatabase');
    });
    
    
    it('should have title', () => {
        goToPage('about');
        cy.getByDataTestId('page-title').should('have.text', 'about');
    });

    it('should have buy me a pizza button', () => {
        goToPage('about');
        cy.getByDataTestId('buymeapizza-text').should('have.text', '🍕 Buy me a pizza');
    });

    it('should have a functional logout button', () => {
        logIn({initialize: true})

        cy.getByDataTestId('cinemadle-date').should('exist');

        goToPage('about');
        cy.getByDataTestId('logout-button').should('have.text', `log out`);
        cy.getByDataTestId('logout-button').click();

        cy.getByDataTestId('user-email').should('not.exist');
    })
});