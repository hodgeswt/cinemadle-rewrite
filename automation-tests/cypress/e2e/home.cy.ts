import { getGuessCard, isoDateNoTime, logIn, makeGuess } from "../support/commands";

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
            makeGuess('Shrek 2');
            
            getGuessCard(0, 'YEAR').then((year) => {
                year.name.should('have.text', 'YEAR');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'RATING').then((rating) => {
                rating.name.should('have.text', 'RATING');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'to-[#00ffcc]');
            })

            getGuessCard(0, 'GENRE').then((genre) => {
                genre.name.should('have.text', 'GENRE');
                genre.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                        expect(texts).to.include('Animation');
                        expect(texts).to.include('Family');
                        expect(texts).to.include('Comedy');
                });
                genre.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'BOX OFFICE').then((boxOffice) => {
                boxOffice.name.should('have.text', 'BOX OFFICE');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CAST').then((cast) => {
                cast.name.should('have.text', 'CAST');
                cast.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                    expect(texts).to.include('Mike Myers');
                    expect(texts).to.include('Eddie Murphy');
                    expect(texts).to.include('Cameron Diaz');
                });
                cast.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.tiledata.should('have.text', 'Director: Andrew Adamson');
                creatives.className.should('contain', 'to-gray-300');
            })
        });

        it('should decrement guess count', () => {
            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should remove guess from suggested', () => {
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').type('Shrek');
            cy.getByDataTestId('guess-Shrek-2-button').should('not.exist');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            logIn({initialize: true});
        })

        it('should render a guess', () => {
            makeGuess('Shrek 2');
            
            getGuessCard(0, 'YEAR').then((year) => {
                year.name.should('have.text', 'YEAR');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'RATING').then((rating) => {
                rating.name.should('have.text', 'RATING');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'to-[#00ffcc]');
            })

            getGuessCard(0, 'GENRE').then((genre) => {
                genre.name.should('have.text', 'GENRE');
                genre.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                        expect(texts).to.include('Animation');
                        expect(texts).to.include('Family');
                        expect(texts).to.include('Comedy');
                });
                genre.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'BOX OFFICE').then((boxOffice) => {
                boxOffice.name.should('have.text', 'BOX OFFICE');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CAST').then((cast) => {
                cast.name.should('have.text', 'CAST');
                cast.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                    expect(texts).to.include('Mike Myers');
                    expect(texts).to.include('Eddie Murphy');
                    expect(texts).to.include('Cameron Diaz');
                });
                cast.className.should('contain', 'to-gray-300');
            })

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.tiledata.should('have.text', 'Director: Andrew Adamson');
                creatives.className.should('contain', 'to-gray-300');
            })
        });

        // TODO: re-enable when payments are implemented
        // it('should show zero visual clues', () => {
        //     cy.getByDataTestId('visualcluesremaining-text').should('have.text', 'visual clues remaining: 0');
        // });

        it('should decrement guess count', () => {
            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should remove guess from suggested', () => {
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').type('Shrek');
            cy.getByDataTestId('guess-Shrek-2-button').should('not.exist');
        });
    });

});