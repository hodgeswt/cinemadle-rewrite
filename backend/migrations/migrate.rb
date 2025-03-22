#!/usr/bin/env ruby

require 'date'
require 'pg'
require 'yaml'

module Migrate
  CREDS_FILE = './credentials.yaml'.freeze

  class Credentials
    attr_accessor :db_name, :user, :password, :host, :port

    def self.validate_input?(db_name, user, password, host, port)
      a = Migrate.validate_string? db_name
      b = Migrate.validate_string? user
      c = Migrate.validate_string? password
      d = Migrate.validate_string? host
      e = Migrate.validate_string? port

      a && b && c && d && e
    end

    def initialize(db_name, user, password, host, port)
      unless Credentials.validate_input?(db_name, user, password, host, port)
        raise 'Expected db_name, user, password, host, and port to be non-empty'
      end

      @db_name = db_name
      @user = user
      @password = password
      @host = host
      @port = port
    end

    def to_s
      "db_name: #{db_name}, user: #{user}, password: ***, host: #{host}, port: #{port}"
    end
  end

  def self.set_credentials?(db_name, user, password, host, port)
    return false unless Credentials.validate_input?(db_name, user, password, host, port)

    c = Credentials.new(db_name, user, password, host, port)

    File.write(CREDS_FILE, c.to_yaml)
    true
  end

  def self.with_db(c)
    raise 'Invalid credentials' unless c.instance_of? Credentials

    db = PG.connect(
      dbname: c.db_name,
      user: c.user,
      password: c.password,
      host: c.host,
      port: c.port
    )

    begin
      yield db
    ensure
      db.close
    end
  end

  def self.validate_string?(value)
    unless value.instance_of? String
      puts 'Expected string input.'
      return false
    end

    re = /^\s*$/

    if re.match?(value)
      puts "Expected value #{value} to be non-empty"
      return false
    end

    true
  end

  def self.generate_migration?(description)
    return false unless validate_string?(description)

    d = DateTime.now.to_s.gsub('-', '').gsub(':', '').gsub('+', '')

    file_description = description.gsub ' ', '_'
    file_path = "sql/#{d}_#{file_description}.sql"

    File.open(file_path, 'w') do |f|
      insert = "\n\nINSERT INTO migration_history (migration)"
      insert = "#{insert}\nVALUES ('#{d}_#{file_description}')\nON CONFLICT (migration) DO NOTHING;"
      f.write("-- DB migration #{description} generated on #{DateTime.now}#{insert}")
    end

    puts "Generated migration #{file_path}"
    true
  end

  def self.apply_all?
    Dir.glob('./sql/*.sql') do |migration|
      return false unless apply_migration? migration
    end

    true
  end

  def self.apply_migration?(name)
    return false unless validate_string?(name)

    name.gsub!(%r{^(\./)?sql/}, '')
    name = "./sql/#{name}"
    unless File.exist? name
      puts "Migration #{name} does not exist"
      return false
    end

    script = File.open(name, 'rb', &:read)
    credentials = YAML.load_file(CREDS_FILE, permitted_classes: [Migrate::Credentials])
    puts "Connecting with #{credentials}"
    with_db(credentials) do |db|
      db.exec(script)
    rescue PG::Error
      puts 'DB error'
      exit 1
    end

    puts 'Migration successful'
    true
  end
end
