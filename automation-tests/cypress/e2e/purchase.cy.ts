import { goToPage, logIn } from "../support/commands";

describe('home page', () => {
    before(() => {
        cy.customTask('destroyDatabase');
    });

    beforeEach(() => {
        cy.init();
        logIn({initialize: true})
        goToPage('purchase');
    })

    afterEach(() => {
        cy.customTask('destroyDatabase');
    });

    it('should show no items purchased', () => {
        cy.getByDataTestId('noitems-text').should('have.text', 'no items yet. purchase some to get started!');
    });

    it('should redirect to stripe on purchase', () => {
        
        cy.getByDataTestId('visualcluepurchase-button').click();

        cy.origin('https://checkout.stripe.com', () => {
            cy.on('uncaught:exception', (e) => {
                return false
            })

            cy.contains('Ten Visual Clues').should('exist');

            cy.get('.CheckoutInput[name="email"]').type('asdf@asdf.com');
            cy.get('.Button[aria-label="Pay with card"]').click({force: true});
            cy.get('.CheckoutInput[name="cardNumber"]').should('exist');
            cy.get('.CheckoutInput[name="cardNumber"]').type('4242424242424242');
            cy.get('.CheckoutInput[name="cardExpiry"]').type('0727');
            cy.get('.CheckoutInput[name="cardCvc"]').type('777');
            cy.get('.CheckoutInput[name="billingName"]').type('asdf');
            cy.get('.CheckoutInput[name="billingPostalCode"]').type('33333');
            cy.get('.Checkbox-Input[name="enableStripePass"]').click({force: true});
            cy.get('.SubmitButton-IconContainer').click({force: true});
        })

        cy.wait(8000);

        cy.getByDataTestId('cinemadle-date').should('be.visible');

        goToPage('purchase');
        cy.getByDataTestId('product-name-text').should('have.text', 'visual clue');
        cy.getByDataTestId('product-quantity-text').should('have.text', '10');
    })
});