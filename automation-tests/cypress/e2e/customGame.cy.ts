import { getGuessCard, isoDateNoTime, logIn, logOut, makeGuess } from "../support/commands";

describe('home page', () => {
    before(() => {
        cy.customTask('destroyDatabase');
    });

    beforeEach(() => {
        cy.init();
    })

    describe('logged out', () => {
        it('should not be accessible by menu', () => {
            cy.getByDataTestId('menu-button').should('be.visible').should('be.enabled');
            cy.getByDataTestId('menu-button').click();

            const linkId = `${'create custom game'.replaceAll(' ', '')}-link`;

            cy.getByDataTestId(linkId)
                .should('not.be.visible')
        });

        it('should redirect to home page', () => {
            cy.visit('/customCreate');
            cy.url().should('not.contain', 'customCreate');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            logIn({initialize: true});
            cy.createCustomGame('Shrek 2').then((copiedUrl) => {
              let trimmed = copiedUrl.replace('https://cinemadle.com', '');
              cy.visit(trimmed)
            });
        })

        it('should render a guess', () => {
            makeGuess('Shrek');
            
            getGuessCard(0, 'YEAR').then((year) => {
                year.name.should('have.text', 'YEAR');
                year.tiledata.should('have.text', '2001');
                year.className.should('contain', 'bg-gradient-to-br from-[#ffeb3b] to-[#ffd700]');
            })

            getGuessCard(0, 'RATING').then((rating) => {
                rating.name.should('have.text', 'RATING');
                rating.tiledata.should('have.text', 'PG');
                rating.className.should('contain', 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]');
            })

            getGuessCard(0, 'GENRE').then((genre) => {
                genre.name.should('have.text', 'GENRE');
                genre.tiledata.should($elements => {
                    const texts = $elements.map((_, el) => Cypress.$(el).text()).get();
                        expect(texts).to.include('Animation');
                        expect(texts).to.include('Comedy');
                        expect(texts).to.include('Family');
                });
                genre.className.should('contain', 'bg-gradient-to-br from-[#ffeb3b] to-[#ffd700]');
            })

            getGuessCard(0, 'BOX OFFICE').then((boxOffice) => {
                boxOffice.name.should('have.text', 'BOX OFFICE');
                boxOffice.tiledata.should('have.text', '$490M');
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
                cast.className.should('contain', 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]');
            })

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.tiledata.should('have.text', 'Director: Andrew Adamson');
                creatives.className.should('contain', 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]');
            })
        });

        it('should decrement guess count', () => {
            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeGuess('Shrek');

            cy.getByDataTestId('guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should remove guess from suggested', () => {
            makeGuess('Shrek');

            cy.getByDataTestId('guess-input').type('Shrek');
            cy.getByDataTestId('guess-Shrek-2-button').should('not.exist');
        });

        it('should let you win', () => {
          makeGuess('Shrek 2');

          cy.getByDataTestId('customgame-youwin')
            .should('exist')
            .should('be.visible');
        });
    });

});