CREATE TABLE author (
    id        INTEGER PRIMARY KEY,
    full_name TEXT NOT NULL,
    email     TEXT,
    bio       TEXT
);

CREATE TABLE book (
    id          INTEGER PRIMARY KEY,
    title       TEXT NOT NULL,
    author_id   INTEGER NOT NULL,
    isbn        TEXT,
    price       REAL,
    published   TEXT,
    FOREIGN KEY (author_id) REFERENCES author(id)
);

CREATE UNIQUE INDEX ux_book_isbn ON book(isbn);
CREATE INDEX ix_book_author ON book(author_id);

INSERT INTO author (full_name, email, bio) VALUES
    ('Ahmet Yılmaz', 'ahmet@example.com', 'Tarih yazarı'),
    ('Zeynep Kaya', NULL, NULL);

INSERT INTO book (title, author_id, isbn, price, published) VALUES
    ('Osmanlı''da İlim', 1, '978-1', 120.0, '2020-01-01'),
    ('Tasavvuf Üzerine', 1, '978-2', 95.5, '2021-06-15'),
    ('Modern Roman', 2, '978-3', 80.0, '2019-03-10');