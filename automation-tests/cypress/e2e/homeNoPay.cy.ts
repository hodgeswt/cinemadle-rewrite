import { logIn, makeGuess } from "../support/commands";

describe('home page', () => {
    before(() => {
        cy.customTask('destroyDatabase');
        cy.customTask('rigMovie');
    });

    after(() => {
        cy.customTask('unrigMovie');
    })

    beforeEach(() => {
        cy.init();
        cy.request({
            method: 'GET',
            url: `${Cypress.env().backendUrl}/api/cinemadle/featureFlags`,
            failOnStatusCode: true
        }).then(r => expect(r.body.featureFlags.PaymentsEnabled).to.eq(false));
    })

    afterEach(() => {
        cy.customTask('destroyDatabase');
    });

    describe('it should allow visual clues when payments disabled', () => {
        it('should show payments disabled', () => {
            logIn({initialize: true});

            const titles = ['Shrek', 'Shrek 2', 'Shrek Forever After', 'John Wick', 'The Avengers', 'Iron Man'];

            for (const title of titles) {
                makeGuess(title);
            }

            cy.getByDataTestId('visualclue-button').should('have.text', 'view visual clue');
            cy.getByDataTestId('visualclue-button').click();

            cy.getByDataTestId('visualclue-image').should('exist');
            cy.getByDataTestId('visualclue-image', {timeout: 10000}).should('be.visible');
        });
    });
});