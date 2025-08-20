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
            
            getGuessCard(0, 'box office').then((boxOffice) => {
                boxOffice.name.should('have.text', 'box office');
                boxOffice.arrowdown1.should('exist');
                boxOffice.arrowdown2.should('exist');
                boxOffice.arrowup1.should('not.exist');
                boxOffice.arrowup2.should('not.exist');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'bg-gray-300');
            })
            

            getGuessCard(0, 'creatives').then((creatives) => {
                creatives.name.should('have.text', 'creatives');
                creatives.arrowdown1.should('not.exist');
                creatives.arrowdown2.should('not.exist');
                creatives.arrowup1.should('not.exist');
                creatives.arrowup2.should('not.exist');
                creatives.tiledata.should('have.text', 'Director: Andrew Adamson');
                creatives.className.should('contain', 'bg-gray-300');
            })

            getGuessCard(0, 'rating').then((rating) => {
                rating.name.should('have.text', 'rating');
                rating.arrowdown1.should('not.exist');
                rating.arrowdown2.should('not.exist');
                rating.arrowup1.should('not.exist');
                rating.arrowup2.should('not.exist');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'bg-green-300');
            })
            

            getGuessCard(0, 'genre').then((genre) => {
                genre.name.should('have.text', 'genre');
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
                genre.className.should('contain', 'bg-gray-300');
            })

            getGuessCard(0, 'cast').then((cast) => {
                cast.name.should('have.text', 'cast');
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
                cast.className.should('contain', 'bg-gray-300');
            })
            

            getGuessCard(0, 'year').then((year) => {
                year.name.should('have.text', 'year');
                year.arrowdown1.should('exist');
                year.arrowdown2.should('exist');
                year.arrowup1.should('not.exist');
                year.arrowup2.should('not.exist');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'bg-gray-300');
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
            
            getGuessCard(0, 'box office').then((boxOffice) => {
                boxOffice.name.should('have.text', 'box office');
                boxOffice.arrowdown1.should('exist');
                boxOffice.arrowdown2.should('exist');
                boxOffice.arrowup1.should('not.exist');
                boxOffice.arrowup2.should('not.exist');
                boxOffice.tiledata.should('have.text', '$935M');
                boxOffice.className.should('contain', 'bg-gray-300');
            })
            

            getGuessCard(0, 'creatives').then((creatives) => {
                creatives.name.should('have.text', 'creatives');
                creatives.arrowdown1.should('not.exist');
                creatives.arrowdown2.should('not.exist');
                creatives.arrowup1.should('not.exist');
                creatives.arrowup2.should('not.exist');
                creatives.tiledata.should('have.text', 'Director: Andrew Adamson');
                creatives.className.should('contain', 'bg-gray-300');
            })

            getGuessCard(0, 'rating').then((rating) => {
                rating.name.should('have.text', 'rating');
                rating.arrowdown1.should('not.exist');
                rating.arrowdown2.should('not.exist');
                rating.arrowup1.should('not.exist');
                rating.arrowup2.should('not.exist');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'bg-green-300');
            })
            

            getGuessCard(0, 'genre').then((genre) => {
                genre.name.should('have.text', 'genre');
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
                genre.className.should('contain', 'bg-gray-300');
            })

            getGuessCard(0, 'cast').then((cast) => {
                cast.name.should('have.text', 'cast');
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
                cast.className.should('contain', 'bg-gray-300');
            })
            

            getGuessCard(0, 'year').then((year) => {
                year.name.should('have.text', 'year');
                year.arrowdown1.should('exist');
                year.arrowdown2.should('exist');
                year.arrowup1.should('not.exist');
                year.arrowup2.should('not.exist');
                year.tiledata.should('have.text', '2004');
                year.className.should('contain', 'bg-gray-300');
            })
        });

        it('should show zero visual clues', () => {
            cy.getByDataTestId('visualcluesremaining-text').should('have.text', 'visual clues remaining: 0');
        });
    });

});