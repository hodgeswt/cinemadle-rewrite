require 'date'

module Migrate
  def self.validate_description(description)
    unless description.instance_of? String
      puts 'Expected string input for description.'
      return false
    end

    re = /^\w*$/

    return true unless re.match?(description)

    puts 'Expected description to be non-empty'
    false
  end

  def self.generate_migration(description)
    return false unless validate_description(description)

    d = DateTime.now.to_s.gsub('-', '').gsub(':', '').gsub('+', '')

    file_description = description.gsub ' ', '_'
    file_path = "sql/#{d}_#{file_description}.sql"

    File.open(file_path, 'w') do |f|
      f.write("-- DB migration #{description} generated on #{DateTime.now}")
    end

    puts "Generated migration #{file_path}"
    true
  end
end
