require_relative 'migrate'

if ARGV.empty?
  puts "Expected a subcommand. Run 'ruby cli.rb help' for instructions"
  exit 1
end

subcommand = ARGV[0]

if subcommand == 'help'
  puts 'cinemadle migration cli'
  puts ''
  puts "Usage: 'ruby cli.rb (help | new [\"description\"] | apply [all])'"
  puts ''
  puts 'Example:'
  puts ''
  puts 'ruby cli.rb help'
  puts '\t Prints this message'
  puts 'ruby cli.rb new "add user table"'
  puts '\t Creates a new migration script called "add user table"'
  exit 0
end

if subcommand == 'new'
  if ARGV.length < 2
    puts 'Expected one sub-argument'
    exit 1
  end

  puts 'Creating new migration...'
  success = Migrate.generate_migration ARGV[1]

  unless success
    puts 'Failed to create migration'
    exit 1
  end

  exit 0
end
