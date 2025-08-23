import { getGuessCard, isoDateNoTime, logIn } from "../support/commands";

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
    })

    afterEach(() => {
        cy.customTask('destroyDatabase');
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

            cy.getByDataTestId('guess-0-title').should('have.text', 'Shrek 2');
            
            getGuessCard(0, 'BOX OFFICE').then((boxOffice) => {
                boxOffice.name.should('have.text', 'BOX OFFICE');
                boxOffice.arrowdown1.should('exist');
                boxOffice.arrowdown2.should('exist');
                boxOffice.arrowup1.should('not.exist');
                boxOffice.arrowup2.should('not.exist');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'to-gray-300');
            })
            

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.arrowdown1.should('not.exist');
                creatives.arrowdown2.should('not.exist');
                creatives.arrowup1.should('not.exist');
                creatives.arrowup2.should('not.exist');
                creatives.tiledata.should('have.text', 'Director: Conrad Vernon');
                creatives.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'RATING').then((rating) => {
                rating.name.should('have.text', 'RATING');
                rating.arrowdown1.should('not.exist');
                rating.arrowdown2.should('not.exist');
                rating.arrowup1.should('not.exist');
                rating.arrowup2.should('not.exist');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'to-[#00ffcc]');
            })
            

            getGuessCard(0, 'GENRE').then((genre) => {
                genre.name.should('have.text', 'GENRE');
                genre.arrowdown1.should('not.exist');
                genre.arrowdown2.should('not.exist');
                genre.arrowup1.should('not.exist');
                genre.arrowup2.should('not.exist');
                genre.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                        expect(texts).to.include('Animation');
                        expect(texts).to.include('Family');
                        expect(texts).to.include('Comedy');
                });
                genre.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CAST').then((cast) => {
                cast.name.should('have.text', 'CAST');
                cast.arrowdown1.should('not.exist');
                cast.arrowdown2.should('not.exist');
                cast.arrowup1.should('not.exist');
                cast.arrowup2.should('not.exist');
                cast.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                    expect(texts).to.include('Mike Myers');
                    expect(texts).to.include('Eddie Murphy');
                    expect(texts).to.include('Cameron Diaz');
                });
                cast.className.should('contain', 'to-gray-300');
            })
            

            getGuessCard(0, 'YEAR').then((year) => {
                year.name.should('have.text', 'YEAR');
                year.arrowdown1.should('exist');
                year.arrowdown2.should('exist');
                year.arrowup1.should('not.exist');
                year.arrowup2.should('not.exist');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'to-gray-300');
            })
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            logIn({initialize: true});
        })

        it('should render a guess', () => {
            cy.getByDataTestId('guess-input').type('Shrek 2');
            cy.getByDataTestId('submit-button').click();

            cy.getByDataTestId('guess-0-title').should('have.text', 'Shrek 2');
            
            getGuessCard(0, 'BOX OFFICE').then((boxOffice) => {
                boxOffice.name.should('have.text', 'BOX OFFICE');
                boxOffice.arrowdown1.should('exist');
                boxOffice.arrowdown2.should('exist');
                boxOffice.arrowup1.should('not.exist');
                boxOffice.arrowup2.should('not.exist');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'to-gray-300');
            })
            

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.arrowdown1.should('not.exist');
                creatives.arrowdown2.should('not.exist');
                creatives.arrowup1.should('not.exist');
                creatives.arrowup2.should('not.exist');
                creatives.tiledata.should('have.text', 'Director: Conrad Vernon');
                creatives.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'RATING').then((rating) => {
                rating.name.should('have.text', 'RATING');
                rating.arrowdown1.should('not.exist');
                rating.arrowdown2.should('not.exist');
                rating.arrowup1.should('not.exist');
                rating.arrowup2.should('not.exist');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'to-[#00ffcc]');
            })
            

            getGuessCard(0, 'GENRE').then((genre) => {
                genre.name.should('have.text', 'GENRE');
                genre.arrowdown1.should('not.exist');
                genre.arrowdown2.should('not.exist');
                genre.arrowup1.should('not.exist');
                genre.arrowup2.should('not.exist');
                genre.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                        expect(texts).to.include('Animation');
                        expect(texts).to.include('Family');
                        expect(texts).to.include('Comedy');
                });
                genre.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CAST').then((cast) => {
                cast.name.should('have.text', 'CAST');
                cast.arrowdown1.should('not.exist');
                cast.arrowdown2.should('not.exist');
                cast.arrowup1.should('not.exist');
                cast.arrowup2.should('not.exist');
                cast.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                    expect(texts).to.include('Mike Myers');
                    expect(texts).to.include('Eddie Murphy');
                    expect(texts).to.include('Cameron Diaz');
                });
                cast.className.should('contain', 'to-gray-300');
            })
            

            getGuessCard(0, 'YEAR').then((year) => {
                year.name.should('have.text', 'YEAR');
                year.arrowdown1.should('exist');
                year.arrowdown2.should('exist');
                year.arrowup1.should('not.exist');
                year.arrowup2.should('not.exist');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'to-gray-300');
            })
        });

        it('should show zero visual clues', () => {
            cy.getByDataTestId('visualcluesremaining-text').should('have.text', 'visual clues remaining: 0');
        });
    });

});