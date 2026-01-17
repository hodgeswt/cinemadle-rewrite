Cypress.Commands.add('getByDataTestId', (dataTestId: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.get(`[data-testid="${dataTestId}"]`, options);
})

Cypress.Commands.add('maybeGetByDataTestId', (dataTestId: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.maybeGet(`[data-testid="${dataTestId}"]`, options);
})

Cypress.Commands.add('maybeGet', (selector: string, options?: Partial<Cypress.Loggable & Cypress.Timeoutable & Cypress.Withinable & Cypress.Shadow>): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.get('body').then($body => {
        if ($body.find(selector).length > 0) {
            return cy.get(selector, options);
        }
        return cy.wrap(null);
    });
})

Cypress.Commands.add('customTask', (task: string) => {
    const backendUrl = Cypress.env().backendUrl;
    
    switch(task) {
        case 'destroyDatabase':
            return cy.request({
                method: 'DELETE',
                url: `${backendUrl}/api/cinemadle/destroy`,
                failOnStatusCode: true
            }).then(r => expect(r.status).to.eq(200));
        case 'rigMovie':
            return cy.request({
                method: 'GET',
                url: `${backendUrl}/api/cinemadle/rig/85`,
                failOnStatusCode: true
            }).then(r => expect(r.status).to.eq(200));
        case 'unrigMovie':
            return cy.request({
                method: 'GET',
                url: `${backendUrl}/api/cinemadle/rig/undo`,
                failOnStatusCode: true
            }).then(r => expect(r.status).to.eq(200));
    }
});

Cypress.Commands.add('init', () => {
    cy.visit('/');
    cy.getByDataTestId('guess-input', {timeout: 10000}).should('exist');
});

export const goToPage = (page: string) => {
    // Wait for menu button to be ready
    cy.getByDataTestId('menu-button').should('be.visible').should('be.enabled');
    cy.getByDataTestId('menu-button').click();

    // Create link ID
    const linkId = `${page.replaceAll(' ', '')}-link`;
    
    // Wait for menu to be fully ready and link to be clickable
    cy.getByDataTestId(linkId)
        .should('be.visible')
        .should('not.be.disabled')
        .click();

    // Verify navigation 
    if (page !== 'home') {
        cy.getByDataTestId('page-title')
            .should('be.visible')
            .should('have.text', page);
    }
}

export type LogInParams = {
    username?: string,
    password?: string,
    initialize?: boolean,
}

export const logIn = (params: LogInParams): LogInParams => {
    const username = params.username !== undefined ? params.username : 'asdf@asdf.com';
    const password = params.password !== undefined ? params.password : 'Password1$';
    const initialize = params.initialize !== undefined ? params.initialize : false;

    if (initialize) {
        goToPage('sign up');
        cy.getByDataTestId('email-input').type(username);
        cy.getByDataTestId('password-input').type(password);
        cy.getByDataTestId('passwordconfirm-input').type(password);
        cy.getByDataTestId('signup-button').click();

        cy.getByDataTestId('page-title').should('have.text', 'log in');
    }

    if (!initialize) {
        goToPage('log in');
    }

    cy.getByDataTestId('email-input').type(username);
    cy.getByDataTestId('password-input').type(password);
    cy.getByDataTestId('login-button').click();


    cy.getByDataTestId('cinemadle-date', {timeout: 10000}).should('exist');

    return { username: username, password: password, initialize: initialize } as LogInParams;
}

export const isoDateNoTime = (): string => {

    const date = new Date();
    const options: Intl.DateTimeFormatOptions = {
        timeZone: 'America/New_York',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
    };

    const formatter = new Intl.DateTimeFormat('en-US', options);
    const formattedParts = formatter.formatToParts(date);

    const year = formattedParts.find(part => part.type === 'year')?.value;
    const month = formattedParts.find(part => part.type === 'month')?.value;
    const day = formattedParts.find(part => part.type === 'day')?.value;

    return `${year}-${month}-${day}`;
}

export type GuessCardData = {
    className: Cypress.Chainable<string>,
    name: Cypress.Chainable<JQuery<HTMLElement>>,
    arrowdown1: Cypress.Chainable<JQuery<HTMLElement>>,
    arrowdown2: Cypress.Chainable<JQuery<HTMLElement>>,
    arrowup1: Cypress.Chainable<JQuery<HTMLElement>>,
    arrowup2: Cypress.Chainable<JQuery<HTMLElement>>,
    direction: Cypress.Chainable<JQuery<HTMLElement>>,
    tiledata: Cypress.Chainable<JQuery<HTMLElement>>,
}

export const getGuessCard = (cardId: number, category: string) => {
    // Start the chain with cy.wrap to ensure we're in Cypress land
    return cy.wrap(null).then(() => {
        return cy.contains(category.toUpperCase())
            .invoke('attr', 'data-testid')
            .then(x => {
                const splits = (x as string).split('-');
                if (splits.length < 3) {
                    throw new Error('test id did not match expectations');
                }
                const cardIndex = splits[2];

                return cy.get(`[data-testid^="card-${cardId}-${cardIndex}-tiledata"]`).then($tiledata => {
                    return cy.wrap({
                        className: cy.contains(category.toUpperCase())
                            .parent()
                            .parent()
                            .invoke('attr', 'class'),
                        name: cy.getByDataTestId(`card-${cardId}-${cardIndex}-title-text`),
                        arrowdown1: cy.maybeGetByDataTestId(`card-${cardId}-${cardIndex}-arrowdown-1`),
                        arrowdown2: cy.maybeGetByDataTestId(`card-${cardId}-${cardIndex}-arrowdown-2`),
                        arrowup1: cy.maybeGetByDataTestId(`card-${cardId}-${cardIndex}-arrowup-1`),
                        arrowup2: cy.maybeGetByDataTestId(`card-${cardId}-${cardIndex}-arrowup-2`),
                        direction: cy.maybeGetByDataTestId(`card-${cardId}-${cardIndex}-direction-text`),
                        tiledata: cy.wrap($tiledata)
                    });
                });
                
            });
    });
};