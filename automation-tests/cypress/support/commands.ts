Cypress.Commands.add('getByDataTestId', (dataTestId: string): Cypress.Chainable<JQuery<HTMLElement>> => {
    return cy.get(`[data-testid="${dataTestId}"]`);
})

export const destroyDatabase = async () => {
    const response = await fetch(`${Cypress.config().baseUrl}/api/cinemadle/destroy`, { method: 'DELETE' });
    if (!response.ok) {
        throw new Error('unable to destroy database');
    }
}

export const goToPage = (page: string) => {
    cy.getByDataTestId('menu-button').click();
    cy.getByDataTestId(`${page.replaceAll(' ', '')}-link`).click();

    if (page !== 'home') {
        cy.getByDataTestId('page-title').should('have.text', page);
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
