import { getGuessCard, isoDateNoTime, logIn, makeGuess } from "../support/commands";

describe('home page', () => {
    before(() => {
        cy.destroyDatabase();
    })

    after(() => {
        cy.unrigMovie();
    })

    beforeEach(() => {
        cy.rigMovie();
        cy.init();
    })

    afterEach(() => {
        cy.destroyDatabase();
    });

    describe('logged out', () => {
        it('should have the date', () => {
            cy.getByDataTestId('cinemadle-date').should('have.text', isoDateNoTime());
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
                boxOffice.tiledata.should('have.text', '$933M');
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
                creatives.className.should('contain', 'to-gray-300');
                creatives.tiledata.should('contain', 'Director: Andrew Adamson');
            })
        });

        it('should decrement guess count', () => {
            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should keep guess count decremented after refresh', () => {
            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeGuess('Shrek 2');

            cy.reload();

            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should remove guess from suggested', () => {
            makeGuess('Shrek 2');

            cy.getByDataTestId('guess-input').type('Shrek');
            cy.getByDataTestId('guess-Shrek-2-button').should('not.exist');
        });

        it('should remove guess from suggested after a refresh', () => {
            makeGuess('Shrek 2');

            cy.reload();

            cy.getByDataTestId('guess-input').type('Shrek');
            cy.getByDataTestId('guess-Shrek-2-button').should('not.exist');
        });

        it('should let you win and show the share sheet', () => {
            cy.rigMovie('808');
            cy.reload();

            makeGuess('Shrek 2');
            makeGuess('Shrek');

            cy.getByDataTestId('share-button')
                .should('exist')
                .should('be.visible')
                .click();

            cy.getByDataTestId('results-body-text')
                .should('exist')
                .should('be.visible')
                .should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text().trim()).get();
                    expect(texts.at(0)).to.contain('⬛🟨🟩🟨🟩🟨🟩🟩🟩🟩🟩🟩');
                });
        });

        it('should preserve guesses on a refresh', () => {
            makeGuess('Shrek 2');
            makeGuess('Shrek');

            cy.reload();

            cy.getByDataTestId('guess-0-title')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Shrek');

            cy.getByDataTestId('guess-1-title')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Shrek 2');
        });

        it('should show the answer if you lose', () => {
            makeGuess('Shrek 2');
            makeGuess('Shrek');
            makeGuess('Shrek the Third');
            makeGuess('The Avengers');
            makeGuess('Avengers: Endgame');
            makeGuess('Avengers: Age of Ultron');
            makeGuess('Avengers: Infinity War');
            makeGuess('Iron Man');
            makeGuess('Iron Man 2');
            makeGuess('Iron Man 3');

            cy.getByDataTestId('hints-display-header')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Raiders of the Lost Ark!');

            cy.getByDataTestId('hints-display-card-year-content')
                .should('exist')
                .should('be.visible')
                .should('have.text', '1981');

            cy.getByDataTestId('hints-display-card-boxOffice-content')
                .should('exist')
                .should('be.visible')
                .should('have.text', '$390M');

            cy.getByDataTestId('hints-display-card-rating-content')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'PG');

            cy.getByDataTestId('hints-display-card-genre-pill-0')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Adventure');

            cy.getByDataTestId('hints-display-card-genre-pill-1')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Action');

            cy.getByDataTestId('hints-display-card-creatives-pill-0')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Director: Steven Spielberg');

            cy.getByDataTestId('hints-display-card-cast-pill-0')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Harrison Ford');

            cy.getByDataTestId('hints-display-card-cast-pill-1')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Karen Allen');

            cy.getByDataTestId('hints-display-card-cast-pill-2')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Paul Freeman');
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
                boxOffice.tiledata.should('have.text', '$933M');
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
                creatives.className.should('contain', 'to-gray-300');
                creatives.tiledata.should('contain', 'Director: Andrew Adamson');
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

        it('should let you win and show the share sheet', () => {
            cy.rigMovie('808');
            cy.reload();

            makeGuess('Shrek 2');
            makeGuess('Shrek');

            cy.getByDataTestId('share-button')
                .should('exist')
                .should('be.visible')
                .click();

            cy.getByDataTestId('results-body-text')
                .should('exist')
                .should('be.visible')
                .should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text().trim()).get();
                    expect(texts.at(0)).to.contain('⬛🟨🟩🟨🟩🟨🟩🟩🟩🟩🟩🟩');
                });
        });

        it('should preserve guesses on a refresh', () => {
            makeGuess('Shrek 2');
            makeGuess('Shrek');

            cy.reload();

            cy.getByDataTestId('guess-0-title')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Shrek');

            cy.getByDataTestId('guess-1-title')
                .should('exist')
                .should('be.visible')
                .should('have.text', 'Shrek 2');
        });

        it('should show a visual clue', () => {
            const titles = ['Shrek', 'Shrek 2', 'Shrek Forever After', 'John Wick', 'The Avengers', 'Iron Man'];

            for (const title of titles) {
                makeGuess(title);
            }

            cy.getByDataTestId('visualclue-button').should('have.text', 'view visual clue');
            cy.getByDataTestId('visualclue-button').click();

            cy.getByDataTestId('visualclue-image').should('exist');
            cy.getByDataTestId('visualclue-image', {timeout: 10000}).should('be.visible');
        })
    });

});