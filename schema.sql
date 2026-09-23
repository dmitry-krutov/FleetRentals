CREATE TABLE IF NOT EXISTS vehicles (
    id uuid PRIMARY KEY,
    license_plate varchar(32) NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'Available',
    CONSTRAINT vehicles_license_plate_not_blank CHECK (length(btrim(license_plate)) > 0),
    CONSTRAINT vehicles_status_check CHECK (status IN ('Available', 'Rented'))
);

CREATE UNIQUE INDEX IF NOT EXISTS vehicles_license_plate_unique ON vehicles (upper(license_plate));

CREATE TABLE IF NOT EXISTS drivers (
    id uuid PRIMARY KEY,
    name varchar(200) NOT NULL,
    CONSTRAINT drivers_name_not_blank CHECK (length(btrim(name)) > 0)
);

CREATE TABLE IF NOT EXISTS rentals (
    id uuid PRIMARY KEY,
    vehicle_id uuid NOT NULL,
    driver_id uuid NOT NULL,
    started_at_utc timestamptz NOT NULL,
    finished_at_utc timestamptz NULL,
    CONSTRAINT rentals_vehicle_fk FOREIGN KEY (vehicle_id) REFERENCES vehicles (id),
    CONSTRAINT rentals_driver_fk FOREIGN KEY (driver_id) REFERENCES drivers (id),
    CONSTRAINT rentals_time_order_check CHECK (finished_at_utc IS NULL OR finished_at_utc >= started_at_utc)
);

CREATE UNIQUE INDEX IF NOT EXISTS rentals_active_vehicle_unique
    ON rentals (vehicle_id) WHERE finished_at_utc IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS rentals_active_driver_unique
    ON rentals (driver_id) WHERE finished_at_utc IS NULL;
