require_relative 'migrate'

if ARGV.empty?
  puts "Expected a subcommand. Run 'ruby cli.rb help' for instructions"
  exit 1
end

subcommand = ARGV[0]

if subcommand == 'help'
  puts 'cinemadle migration cli'
  puts ''
  puts "Usage: 'ruby cli.rb (help | creds [db_name user password host port] | new [\"description\"] | apply [all])'"
  puts ''
  puts 'Example:'
  puts ''
  puts 'ruby cli.rb help'
  puts "\tPrints this message"
  puts 'ruby cli.rb new "add user table"'
  puts "\tCreates a new migration script called \"add user table\""
  puts 'ruby cli.rb creds database admin Password1 127.0.0.1 5432'
  puts "\tSets credentials for cli to PG database \"database\" with user admin/Password1 at localhost:5432"
  puts 'ruby cli.rb apply file_name'
  puts "\tApplies migration \"file_name\""
  puts 'ruby cli.rb apply all'
  puts "\tApplies all migrations"
  exit 0
end

if subcommand == 'new'
  if ARGV.length < 2
    puts 'Expected one sub-argument'
    exit 1
  end

  puts 'Creating new migration...'
  success = Migrate.generate_migration? ARGV[1]

  unless success
    puts 'Failed to create migration'
    exit 1
  end

  exit 0
end

if subcommand == 'creds'
  if ARGV.length != 6
    puts 'Expected db_name, user, password, host, and port'
    exit 1
  end

  puts "Creating and saving credentials to #{Migrate::CREDS_FILE}"

  unless Migrate.set_credentials?(ARGV[1], ARGV[2], ARGV[3], ARGV[4], ARGV[5])
    puts 'Failed to set credentials'
    exit 1
  end

  puts 'Credentials created'
  exit 0
end

if subcommand == 'apply'
  if ARGV.length != 2
    puts 'Expected migration name or "all"'
    exit 1
  end

  if ARGV[1] == 'all'
    unless Migrate.apply_all?
      puts 'Failed to apply all migrations'
      exit 1
    end
  else
    unless Migrate.apply_migration?(ARGV[1])
      puts 'Failed to apply migration'
      exit 1
    end
  end

  puts 'Migration applied'
  exit 0
end
