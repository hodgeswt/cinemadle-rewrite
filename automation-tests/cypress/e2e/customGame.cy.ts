import { getGuessCard, logIn, makeCustomGuess } from "../support/commands";

describe('custom game', () => {
    before(() => {
        cy.customTask('destroyDatabase');
    });

    beforeEach(() => {
        cy.init();
        cy.customTask('destroyDatabase');
    })

    describe('logged out', () => {
        it('should not be accessible by menu', () => {
            cy.getByDataTestId('menu-button').should('be.visible').should('be.enabled');
            cy.getByDataTestId('menu-button').click();

            const linkId = 'customcreate-link';

            cy.getByDataTestId(linkId).should('not.exist')
        });

        it('should redirect to home page', () => {
            cy.visit('/customCreate');
            cy.url().should('not.contain', 'customCreate');
        });
    });

    describe('logged in', () => {
        beforeEach(() => {
            cy.customTask('destroyDatabase');

            logIn({initialize: true});

            cy.createCustomGame('Shrek 2').then((copiedUrl) => {
              let path = new URL(String(copiedUrl)).pathname + new URL(String(copiedUrl)).search;
              path = path.replace("https://cinemadle.com", Cypress.env().frontendUrl);
              cy.log('received url', path)
              cy.visit(path);
            });
        })

        it('should render a guess', () => {
            makeCustomGuess('Shrek');
            
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
                    const texts = $elements.map((_, el) => Cypress.$(el).text().trim()).get();
                    expect(texts).to.include('Animation');
                    expect(texts).to.include('Comedy');
                    expect(texts).to.include('Fantasy');
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
                    const texts = $elements.map((_, el) => Cypress.$(el).text().trim()).get();
                    expect(texts).to.include('Mike Myers');
                    expect(texts).to.include('Eddie Murphy');
                    expect(texts).to.include('Cameron Diaz');
                });
                cast.className.should('contain', 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]');
            })

            getGuessCard(0, 'CREATIVES').then((creatives) => {
                creatives.name.should('have.text', 'CREATIVES');
                creatives.className.should('contain', 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]');
                creatives.tiledata.invoke('text').should(text => {
                    if (text.includes('Conrad Vernon')) {
                        expect(text).to.contain('Director: Conrad Vernon');
                    } else {
                        expect(text).to.contain('Director: Andrew Adamson');
                    }
                });
            })
        });

        it('should decrement guess count', () => {
            cy.getByDataTestId('customgame-guess-input').should('have.attr', 'placeholder', 'Guess... 10 remaining')
            
            makeCustomGuess('Shrek');

            cy.getByDataTestId('customgame-guess-input').should('have.attr', 'placeholder', 'Guess... 9 remaining')
        });

        it('should remove guess from suggested', () => {
            makeCustomGuess('Shrek');

            cy.getByDataTestId('customgame-guess-input').type('Shrek');
            cy.getByDataTestId('customgame-guess-Shrek-button').should('not.exist');
        });

        it('should let you win', () => {
          makeCustomGuess('Shrek 2');

          cy.getByDataTestId('customgame-youwin')
            .should('exist')
            .should('be.visible');
        });
    });

});