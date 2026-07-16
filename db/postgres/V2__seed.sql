-- Dummy seed data. Fake POC credentials — password intentionally equals username.

INSERT INTO users (username, password, display_name) VALUES
    ('alice', 'alice', 'Alice Admin'),
    ('bob',   'bob',   'Bob Editor'),
    ('carol', 'carol', 'Carol Writer'),
    ('dave',  'dave',  'Dave Reader'),
    ('erin',  'erin',  'Erin Writer')
ON conflict (username) do nothing;

-- Posts seeded in a fixed order so ids 1 and 2 match the owner tuples in db/fga/store.fga.yaml.
INSERT INTO posts (title, body, author_username) VALUES
    (
        'First Light on Eagle Ridge',
        'Left the trailhead at 5am to catch sunrise from the ridge. Frost on the grass, '
            || 'breath fogging in the headlamp beam. Worth every switchback — the whole valley '
            || 'went gold in about ninety seconds.',
        'carol'
    ),
    (
        'Ultralight Gear Notes from the Sierra',
        'Shaved another pound off the base weight this season, mostly by swapping the '
            || 'trowel and cook kit. Still debating whether the quilt is warm enough below '
            || 'freezing — next trip will tell.',
        'erin'
    );
