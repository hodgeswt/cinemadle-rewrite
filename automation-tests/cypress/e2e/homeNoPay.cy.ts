import { logIn } from "../support/commands";

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
            url: `${Cypress.config().baseUrl}/api/cinemadle/featureFlags`,
            failOnStatusCode: true
        }).then(r => expect(r.body.featureFlags.PaymentsEnabled).to.eq(false));
    })

    afterEach(() => {
        cy.customTask('destroyDatabase');
    });

    describe('it should allow visual clues when payments disabled', () => {
        it('should show payments disabled', () => {
            logIn({initialize: true});

            let titles = ['Shrek', 'Shrek 2', 'Shrek Forever After', 'John Wick', 'The Avengers', 'Iron Man']

            for (const title of titles) {
                cy.getByDataTestId('guess-input').type(title);
                cy.getByDataTestId('submit-button').click();

                cy.getByDataTestId('guess-0-title').should('have.text', title);
            }

            cy.getByDataTestId('visualclue-button').should('have.text', 'view visual clue');
            cy.getByDataTestId('visualclue-button').click();

        });
    });
});