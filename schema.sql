CREATE TABLE IF NOT EXISTS vehicles (
    id uuid PRIMARY KEY,
    license_plate varchar(32) NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'Available',
    CONSTRAINT vehicles_license_plate_not_blank CHECK (length(btrim(license_plate)) > 0),
    CONSTRAINT vehicles_status_check CHECK (status IN ('Available', 'Rented'))
);

CREATE UNIQUE INDEX IF NOT EXISTS vehicles_license_plate_unique ON vehicles (upper(license_plate));
