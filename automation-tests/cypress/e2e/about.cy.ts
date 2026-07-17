import { goToPage } from "../support/commands";

describe('about page', () => {
    before(() => {
        cy.customTask('destroyDatabase');
    });

    beforeEach(() => {
        cy.init();
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
        cy.getByDataTestId('buymeapizza-text').should('have.text', '🍕 buy me a pizza');
    });

});